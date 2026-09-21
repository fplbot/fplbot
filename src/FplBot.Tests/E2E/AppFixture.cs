using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AspNet.Security.OAuth.Discord;
using Discord.Net.HttpClients;
using FakeItEasy;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.PulseLive;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Models;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Hosting;
using FplBot.Integrations.WebPush;
using FplBot.Services.EventHandlers;
using FplBot.Services.WebApi;
using FplBot.Tests.E2E.Discord;
using FplBot.Tests.E2E.Slack.SlackSubscriptions;
using FplBot.Tests.E2E.Web;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Endpoints.Api.Web;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nest;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FplBot.Tests.E2E;

public class AppFixture : IAsyncLifetime
{
    // One Redis per fixture type, never one shared across them: FlushRedisAsync wipes the whole
    // server, and collections run in parallel, so a flush in one collection would otherwise
    // delete state a test in another collection is still working with.
    private readonly RedisContainer _redis;

    public AppFixture() : this("app-fixture")
    {
    }

    protected AppFixture(string redisReuseId)
    {
        _redis = new RedisBuilder("redis:latest")
            .WithReuse(true)
            .WithLabel("reuse-id", redisReuseId)
            .Build();
    }

    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private IServiceScope _managerScope = null!;
    private ConnectionMultiplexer _multiplexer = null!;
    private CapturingSlackClient _capturingSlackClient = null!;
    private CapturingDiscordClient _capturingDiscordClient = null!;
    private readonly StubDiscordGuildGet _stubDiscordGuildGet = new();

    public SlackMessageCapture SlackCapture { get; } = new();


    public DiscordMessageCapture DiscordCapture { get; } = new();

    public CapturingWebPushSender WebPushCapture { get; } = new();

    public ISlackClient SlackClient { get; private set; } = null!;

    public IBus Bus => _app.Services.GetRequiredService<IBus>();

    // Always scoped, never the root provider — resolving a Scoped service straight from root
    // throws, and there's no way for a caller here to know a service's registered lifetime.
    public IServiceProvider Services => _managerScope.ServiceProvider;
    public ISendEndpointProvider Publisher => _managerScope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
    public ISlackTeamRepository SlackRepo => _managerScope.ServiceProvider.GetRequiredService<ISlackTeamRepository>();
    public IGuildRepository GuildRepo => _managerScope.ServiceProvider.GetRequiredService<IGuildRepository>();
    public IGuildMemberCountRepository GuildMemberCountRepo => _managerScope.ServiceProvider.GetRequiredService<IGuildMemberCountRepository>();
    public IChannelMemberCountRepository ChannelMemberCountRepo => _managerScope.ServiceProvider.GetRequiredService<IChannelMemberCountRepository>();

    public void SlackChannelFails(string channelId, string slackError) =>
        _capturingSlackClient.FailChannel(channelId, slackError);

    public void SetSlackChannelMemberCount(string channelId, int memberCount) =>
        _capturingSlackClient.SetMemberCount(channelId, memberCount);

    public void SetDiscordGuildMemberCount(string guildId, int approximateMemberCount) =>
        _stubDiscordGuildGet.SetApproximateMemberCount(guildId, approximateMemberCount);

    public void DiscordGuildGetFails(string guildId, HttpStatusCode status) =>
        _stubDiscordGuildGet.FailGuild(guildId, status);

    public void RecoverSlackChannel(string channelId) => _capturingSlackClient.RecoverChannel(channelId);

    public void SetSlackChannels(params Slackbot.Net.SlackClients.Http.Models.Responses.ConversationsList.Conversation[] channels) =>
        _capturingSlackClient.SetChannels(channels);

    public void SetDiscordGuildChannels(params global::Discord.Net.HttpClients.DiscordClient.Channel[] channels) =>
        _capturingDiscordClient.SetGuildChannels(channels);

    public void SetSlackAppsUninstallResult(Slackbot.Net.SlackClients.Http.Models.Responses.Response response) =>
        _capturingSlackClient.SetAppsUninstallResult(response);

    public void SetSlackAppsUninstallThrows(Exception exception) => _capturingSlackClient.SetAppsUninstallThrows(exception);

    public void DiscordChannelFails(string channelId, int discordErrorCode) =>
        _capturingDiscordClient.FailChannel(channelId, discordErrorCode);

    public void DiscordChannelFails(string channelId, HttpStatusCode status) =>
        _capturingDiscordClient.FailChannel(channelId, status);

    public void DiscordChannelFails(string channelId, Exception exception) =>
        _capturingDiscordClient.FailChannel(channelId, exception);

    public void RecoverDiscordChannel(string channelId) => _capturingDiscordClient.RecoverChannel(channelId);

    public void ResetChannelOutcomes()
    {
        _capturingSlackClient.Reset();
        _capturingDiscordClient.Reset();
    }

    private static readonly JsonSerializerOptions HttpJson =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private const string RemoteIpHeader = "X-Test-Remote-Ip";

    public Task<HttpResponseMessage> Get(string path) => _client.GetAsync(path, TestContext.Current.CancellationToken);

    public Task<HttpResponseMessage> Send(HttpRequestMessage request) => _client.SendAsync(request, TestContext.Current.CancellationToken);

    public async Task<HttpResponseMessage> GetFrom(string path, string remoteIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(RemoteIpHeader, remoteIp);
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public Task<HttpResponseMessage> Post(string path, object? body = null) =>
        _client.PostAsync(path, AsJson(body), TestContext.Current.CancellationToken);

    public Task<HttpResponseMessage> Put(string path, object? body = null) =>
        _client.PutAsync(path, AsJson(body), TestContext.Current.CancellationToken);

    public Task<HttpResponseMessage> Delete(string path) => _client.DeleteAsync(path, TestContext.Current.CancellationToken);

    public async Task<T> GetJson<T>(string path)
    {
        var response = await Get(path);
        response.EnsureSuccessStatusCode();
        return await ReadJson<T>(response);
    }

    public static async Task<T> ReadJson<T>(HttpResponseMessage response) =>
        (await JsonSerializer.DeserializeAsync<T>(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), HttpJson,
            TestContext.Current.CancellationToken))!;

    private static StringContent AsJson(object? body) =>
        new(JsonSerializer.Serialize(body ?? new { }, HttpJson), Encoding.UTF8, "application/json");

    public async Task<string> SubscribeToWebPush(long? leagueId = null, string? endpoint = null, string? name = null)
    {
        var response = await Post("/api/web/push/subscribe", new
        {
            endpoint = endpoint ?? $"https://push.example.test/{Guid.NewGuid():N}",
            p256dh = "BFakeP256dhKeyForTests",
            auth = "FakeAuthSecret",
            leagueId,
            name
        });
        response.EnsureSuccessStatusCode();
        var body = await ReadJson<SubscribeResponse>(response);
        return body.SubscriberId;
    }

    public async Task<SubscriberStateResponse> GetWebPushState(string subscriberId)
    {
        var response = await GetWebPushRaw(subscriberId, "/api/web/me");
        response.EnsureSuccessStatusCode();
        return await ReadJson<SubscriberStateResponse>(response);
    }

    public Task<HttpResponseMessage> GetWebPushRaw(string subscriberId, string path) =>
        SendWebPush(HttpMethod.Get, subscriberId, path, null);

    public Task<HttpResponseMessage> PutWebPush(string subscriberId, string path, object body) =>
        SendWebPush(HttpMethod.Put, subscriberId, path, body);

    public Task<HttpResponseMessage> PostWebPush(string subscriberId, string path, object? body = null) =>
        SendWebPush(HttpMethod.Post, subscriberId, path, body);

    public Task<HttpResponseMessage> DeleteWebPush(string subscriberId, string path) =>
        SendWebPush(HttpMethod.Delete, subscriberId, path, null);

    private Task<HttpResponseMessage> SendWebPush(HttpMethod method, string subscriberId, string path, object? body)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Subscriber-Id", subscriberId);
        if (body is not null)
        {
            request.Content = AsJson(body);
        }

        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public virtual async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();

        var redisConnStr = _redis.GetConnectionString();
        _multiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnStr + ",allowAdmin=true");
        var redisUrl = $"redis://user:pass@{redisConnStr}";

        _capturingSlackClient = new CapturingSlackClient(SlackCapture);
        SlackClient = _capturingSlackClient;
        var slackClient = SlackClient;
        var fakeSlackClientBuilder = A.Fake<ISlackClientBuilder>();
        A.CallTo(() => fakeSlackClientBuilder.Build(A<string>._)).Returns(slackClient);

        var globalSettings = JsonSerializer.Deserialize<GlobalSettings>(
            TestResources.Boostrap_Static_Json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var fakeGlobalSettings = GlobalSettingsClientBuilder.Returning(globalSettings);

        var fakeFixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fakeFixtureClient.GetFixtures()).Returns([]);

        var fakeLeagueClient = A.Fake<ILeagueClient>();
        A.CallTo(() => fakeLeagueClient.GetClassicLeague(A<int>._, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague
            {
                Properties = new ClassicLeagueProperties { StartEvent = 1 },
                Standings = new ClassicLeagueStandings { Entries = [] }
            });

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["REDIS_URL"] = redisUrl,
                ["OTEL_ENABLED"] = "false",
                ["SKIP_DISCORD_SIGNATURE_VERIFICATION"] = "true"
            })
            .Build();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        // A passing test run should be silent, and nothing asserts on log output. Serilog stays wired up
        // with no sinks because the request-logging middleware resolves its DiagnosticContext; add a
        // WriteTo.Console() here locally when a failure needs the log narrative.
        builder.Configuration.AddConfiguration(config);

        var active = new List<IFplBotService> { new WebApiService(), new EventHandlersService() };
        FplBotApplication.ConfigureServices(builder.Services, config, _multiplexer, builder.Environment, active,
            cfg => cfg.UsingInMemory((ctx, c) =>
            {
                c.ConnectConsumeObserver(BusActivity);
                c.ConfigureEndpoints(ctx);
            }));

        builder.Services.AddSingleton<IConnectionMultiplexer>(_multiplexer);
        builder.Services.AddSingleton(_multiplexer);
        builder.Services.AddStackExchangeRedisCache(o =>
            o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_multiplexer));

        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAdminAuthHandler>(TestAdminAuthHandler.SchemeName, _ => { });
        builder.Services.AddOptions<AuthorizationOptions>().PostConfigure(o =>
            o.AddPolicy("IsAdmin", b => b
                .AddAuthenticationSchemes(TestAdminAuthHandler.SchemeName)
                .RequireAuthenticatedUser()));
        builder.Services.AddHttpClient("Discord.Net.Endpoints.TokenExchange")
            .ConfigurePrimaryHttpMessageHandler(() => new StubDiscordTokenExchange());
        // The admin-login Discord OAuth scheme (AspNet.Security.OAuth.Discord) makes its token-exchange
        // and userinfo calls through its own per-scheme Backchannel, not the named HttpClient above —
        // stub that one too so DiscordLoginTests can drive a real challenge/callback round trip.
        builder.Services.Configure<DiscordAuthenticationOptions>(DiscordAuthenticationDefaults.AuthenticationScheme,
            o => o.BackchannelHttpHandler = new StubDiscordAdminOAuth());
        builder.Services.AddHttpClient(nameof(IPlayerImageClient))
            .ConfigurePrimaryHttpMessageHandler(() => new StubPlayerImages());
        // RefreshGuildMemberCountHandler calls the concrete DiscordClient directly (not the
        // IDiscordClient interface CapturingDiscordClient replaces above), so its own typed
        // HttpClient needs its own stub — same override technique, one call per test guild.
        builder.Services.AddHttpClient<global::Discord.Net.HttpClients.DiscordClient>()
            .ConfigurePrimaryHttpMessageHandler(() => _stubDiscordGuildGet);
        builder.Services.AddSingleton(fakeGlobalSettings);
        builder.Services.AddSingleton(fakeFixtureClient);
        builder.Services.AddSingleton(fakeLeagueClient);
        builder.Services.AddSingleton(A.Fake<ITransfersClient>());
        builder.Services.AddSingleton(A.Fake<IEntryClient>());
        builder.Services.AddSingleton(A.Fake<ILiveClient>());
        builder.Services.AddSingleton(A.Fake<IPulseLiveClient>());
        builder.Services.AddSingleton(A.Fake<IEntryHistoryClient>());
        builder.Services.AddSingleton(A.Fake<IEventStatusClient>());

        builder.Services.RemoveAll<ISlackClientBuilder>();
        builder.Services.AddSingleton(fakeSlackClientBuilder);

        builder.Services.RemoveAll<IDiscordClient>();
        _capturingDiscordClient = new CapturingDiscordClient(DiscordCapture);
        builder.Services.AddSingleton<IDiscordClient>(_capturingDiscordClient);

        builder.Services.RemoveAll<IWebPushSender>();
        builder.Services.AddSingleton<IWebPushSender>(WebPushCapture);

        ConfigureSearchClient(builder.Services);

        // FplBotApplication.WireUpLogging is deliberately not called: a passing test run is silent, and
        // nothing asserts on log output. Call it here locally when a failure needs the log narrative.
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.None);

        _app = builder.Build();
        _app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Headers.TryGetValue(RemoteIpHeader, out var ip) && IPAddress.TryParse(ip, out var parsed))
            {
                ctx.Connection.RemoteIpAddress = parsed;
            }

            await next();
        });
        foreach (var svc in active)
        {
            svc.ConfigureApp(_app);
        }

        _managerScope = _app.Services.CreateScope();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public virtual async ValueTask DisposeAsync()
    {
        _managerScope?.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        _multiplexer?.Dispose();
    }

    public async Task AskSlackbot(string teamId, string channelId, string input)
    {
        var payload = new
        {
            token = "test",
            team_id = teamId,
            api_app_id = "test",
            type = "event_callback",
            event_id = Guid.NewGuid().ToString(),
            event_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            authed_users = new[] { "U0BOT" },
            @event = new
            {
                type = "app_mention",
                text = input,
                user = "U12345",
                channel = channelId,
                ts = "1234567890.123456",
                event_ts = "1234567890.123456"
            }
        };

        var response = await _client.PostAsJsonAsync("/slack/events", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task AskSlackbot(Installation installation, string input, string channelId)
    {
        await AskSlackbot(installation.ExternalId, channelId, input);
    }

    public async Task AskSlackbot(string input)
    {
        var slackInstallation = await SeedInstallation();
        var channelId = slackInstallation.ChannelSubscriptions.First().ChannelId;
        await AskSlackbot(slackInstallation, input, channelId);
    }

    /// <summary>
    ///     Installs a workspace through the real WorkspaceInstallationHandler.Install flow (as opposed to
    ///     SeedInstallation, which writes the domain object straight to the repository) — use this when a
    ///     test wants the real install side effects (AppInstalled published, bare/no-subscriptions state).
    /// </summary>
    public async Task<string> InstallSlackbot(string? teamId = null, string? teamName = null)
    {
        teamId ??= "T" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        teamName ??= "Test Team " + teamId;
        var token = "xoxb-" + Guid.NewGuid().ToString("N");

        await Services.GetRequiredService<IWorkspaceInstallationHandler>()
            .Install(new Workspace(teamId, teamName, token));

        return teamId;
    }

    public async Task<(string Token, string ResponseBody)> AskDiscord(string commandName, string? optionValue = null, string? subCommandName = null,
        string? guildId = null, string? channelId = null, long appPermissions = DiscordPermissions.All)
    {
        guildId ??= Guid.NewGuid().ToString("N");
        channelId ??= Guid.NewGuid().ToString("N");
        var interactionId = Guid.NewGuid().ToString("N");
        var interactionToken = Guid.NewGuid().ToString("N");

        var data = new JsonObject { ["name"] = commandName, ["type"] = 1 };
        if (optionValue != null)
        {
            var innerOption = new JsonObject { ["name"] = "value", ["value"] = optionValue };
            JsonArray options = subCommandName != null
                ? [new JsonObject { ["name"] = subCommandName, ["options"] = new JsonArray(innerOption) }]
                : [innerOption];
            data["options"] = options;
        }

        var payload = new JsonObject
        {
            ["type"] = 2,
            ["id"] = interactionId,
            ["token"] = interactionToken,
            ["guild_id"] = guildId,
            ["channel_id"] = channelId,
            ["app_permissions"] = appPermissions.ToString(),
            ["data"] = data
        };

        var response = await _client.PostAsync("/discord/events",
            new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return (interactionToken, await response.Content.ReadAsStringAsync());
    }

    public BusActivity BusActivity { get; } = new();

    // Waits until every consumer the bus handed a message to has finished and nothing new has
    // started. A test asserting that no message was posted can then read the capture directly:
    // if the handlers are done and the capture is empty, nothing is going to arrive later.
    public long ConsumedSoFar => BusActivity.Snapshot().Consumed;

    public async Task WaitUntilBusIdle()
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var before = BusActivity.Snapshot();
            await Task.Delay(5);
            var after = BusActivity.Snapshot();
            if (after.InFlight == 0 && after.Consumed == before.Consumed)
                return;
        }

        throw new TimeoutException("The bus never went idle.");
    }

    // Publishing returns once the message is on the transport, so an immediate idle check can see a
    // bus that hasn't picked the message up yet. Pass ConsumedSoFar from before the publish and the
    // wait only counts the bus as idle once every published message has been consumed.
    public async Task WaitUntilBusIdle(long consumedBefore, int published = 1)
    {
        await WaitUntil(() => Task.FromResult(ConsumedSoFar >= consumedBefore + published),
            $"The bus never consumed {published} message(s)");
        await WaitUntilBusIdle();
    }

    public static async Task WaitUntil(Func<Task<bool>> condition, string what = "Condition never became true")
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        do
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(2);
        } while (DateTime.UtcNow < deadline);

        throw new TimeoutException(what);
    }

    public async Task<Installation> SeedInstallation(Action<Installation>? configure = null)
    {
        var teamId = "T" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var channelId = "#" + Guid.NewGuid().ToString("N")[..8];
        var token = "xoxb-" + Guid.NewGuid().ToString("N");

        // A real, currently-valid FPL league — some handlers (e.g. captains) call the live
        // FPL API with this id, so it can't be random garbage that 404s.
        var channels = new[] { ChannelSubscription.Load(SubscriptionId.New(), channelId, new ClassicLeagueId(15263), []) };
        var installation = Installation.Load(InstallationId.New(), teamId, "Test Team " + teamId, token, channels);

        configure?.Invoke(installation);

        await Services.GetRequiredService<ISlackTeamRepository>().Save(installation);
        return installation;
    }

    public async Task<Installation> SeedGuildInstallation(int? leagueId = null,
        IEnumerable<EventSubscription>? subscriptions = null, string? channelId = null)
    {
        var guildId = Guid.NewGuid().ToString("N");
        channelId ??= Guid.NewGuid().ToString("N");
        var events = (subscriptions ?? []).Select(s => Enum.Parse<FplEvent>(s.ToString()));
        var channel = ChannelSubscription.Load(SubscriptionId.New(), channelId, leagueId is { } id ? new ClassicLeagueId(id) : null, events);
        var installation = Installation.Load(InstallationId.New(), guildId, "Test Guild " + guildId, token: null, [channel]);

        await Services.GetRequiredService<IGuildRepository>().Save(installation);
        return installation;
    }

    public async Task SeedSearchEntry(EntryItem entry)
    {
        var options = Services.GetRequiredService<IOptions<SearchOptions>>().Value;
        var client = Services.GetRequiredService<IElasticClient>();
        await client.IndexAsync(entry, i => i.Index(options.EntriesIndex).Id(entry.Id));
        await client.Indices.RefreshAsync(options.EntriesIndex);
    }


    // No-op here: only the search-focused subclass (SearchAppFixture) needs a real
    // Elasticsearch-backed IElasticClient; every other AppFixture consumer doesn't touch search.
    public IndexedQueryCapture IndexedQueries { get; } = new();

    protected virtual void ConfigureSearchClient(IServiceCollection services)
    {
        services.RemoveAll<IElasticClient>();
        services.AddSingleton(A.Fake<IElasticClient>());
        services.RemoveAll<IIndexingClient>();
        services.AddSingleton<IIndexingClient>(IndexedQueries);
    }

    public async Task FlushRedisAsync()
    {
        var server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());
        await server.FlushAllDatabasesAsync();
    }

    public async Task Subscribe(string teamId, string channel, params FplEvent[] events)
    {
        var repo = Services.GetRequiredService<ISlackTeamRepository>();
        var installation = await repo.GetInstallation(teamId);
        installation.Subscribe(channel, events);
        await repo.Save(installation);
    }
}

[CollectionDefinition("App")]
public class AppCollection : ICollectionFixture<AppFixture>;
