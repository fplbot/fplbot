using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Discord.Net.HttpClients;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search;
using Fpl.Search.Models;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Hosting;
using FplBot.Services.EventHandlers;
using FplBot.Services.WebApi;
using FplBot.Tests.E2E.Discord;
using FplBot.Tests.E2E.Slack.SlackSubscriptions;
using FplBot.Tests.Helpers;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
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

    public SlackMessageCapture SlackCapture { get; } = new();

    public LogCapture LogCapture { get; } = new();

    public DiscordMessageCapture DiscordCapture { get; } = new();

    public ISlackClient SlackClient { get; private set; } = null!;

    public IBus Bus => _app.Services.GetRequiredService<IBus>();

    // Always scoped, never the root provider — resolving a Scoped service straight from root
    // throws, and there's no way for a caller here to know a service's registered lifetime.
    public IServiceProvider Services => _managerScope.ServiceProvider;
    public ISendEndpointProvider Publisher => _managerScope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
    public ISlackTeamRepository SlackRepo => _managerScope.ServiceProvider.GetRequiredService<ISlackTeamRepository>();
    public IGuildRepository GuildRepo => _managerScope.ServiceProvider.GetRequiredService<IGuildRepository>();

    public void SlackChannelFails(string channelId, string slackError) =>
        _capturingSlackClient.FailChannel(channelId, slackError);

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

    public Task<HttpResponseMessage> Get(string path) => _client.GetAsync(path, TestContext.Current.CancellationToken);

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
        builder.Host.UseSerilog((_, lc) => lc.WriteTo.Console().WriteTo.Sink(LogCapture), preserveStaticLogger: true);
        builder.Configuration.AddConfiguration(config);

        var active = new List<IFplBotService> { new WebApiService(), new EventHandlersService() };
        FplBotApplication.ConfigureServices(builder.Services, config, _multiplexer, builder.Environment, active,
            cfg => cfg.UsingInMemory((ctx, c) => c.ConfigureEndpoints(ctx)));

        builder.Services.AddSingleton<IConnectionMultiplexer>(_multiplexer);
        builder.Services.AddSingleton(_multiplexer);
        builder.Services.AddStackExchangeRedisCache(o =>
            o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_multiplexer));

        builder.Services.AddHttpClient("Discord.Net.Endpoints.TokenExchange")
            .ConfigurePrimaryHttpMessageHandler(() => new StubDiscordTokenExchange());
        builder.Services.AddSingleton(fakeGlobalSettings);
        builder.Services.AddSingleton(fakeFixtureClient);
        builder.Services.AddSingleton(fakeLeagueClient);
        builder.Services.AddSingleton(A.Fake<ITransfersClient>());
        builder.Services.AddSingleton(A.Fake<IEntryClient>());
        builder.Services.AddSingleton(A.Fake<ILiveClient>());
        builder.Services.AddSingleton(A.Fake<IEntryHistoryClient>());
        builder.Services.AddSingleton(A.Fake<IEventStatusClient>());

        builder.Services.RemoveAll<ISlackClientBuilder>();
        builder.Services.AddSingleton(fakeSlackClientBuilder);

        builder.Services.RemoveAll<IDiscordClient>();
        _capturingDiscordClient = new CapturingDiscordClient(DiscordCapture);
        builder.Services.AddSingleton<IDiscordClient>(_capturingDiscordClient);

        ConfigureSearchClient(builder.Services);

        _app = builder.Build();
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

        var response = await _client.PostAsJsonAsync("/events", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task AskSlackbot(Installation installation, string input, string channelId)
    {
        await AskSlackbot(installation.Id, channelId, input);
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

    public static async Task WaitUntil(Func<Task<bool>> condition)
    {
        for (var i = 0; i < 100; i++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Condition never became true");
    }

    public async Task<Installation> SeedInstallation(Action<Installation>? configure = null)
    {
        var teamId = "T" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var channelId = "#" + Guid.NewGuid().ToString("N")[..8];
        var token = "xoxb-" + Guid.NewGuid().ToString("N");

        // A real, currently-valid FPL league — some handlers (e.g. captains) call the live
        // FPL API with this id, so it can't be random garbage that 404s.
        var channels = new[] { ChannelSubscription.Load(channelId, new ClassicLeagueId(15263), []) };
        var installation = Installation.Load(teamId, "Test Team " + teamId, token, channels);

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
        var channel = ChannelSubscription.Load(channelId, leagueId is { } id ? new ClassicLeagueId(id) : null, events);
        var installation = Installation.Load(guildId, "Test Guild " + guildId, token: null, [channel]);

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
    protected virtual void ConfigureSearchClient(IServiceCollection services)
    {
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
