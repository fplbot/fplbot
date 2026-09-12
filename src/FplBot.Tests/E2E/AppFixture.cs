using Discord.Net.Endpoints.Hosting;
using Discord.Net.HttpClients;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search;
using Fpl.Search.Models;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Hosting;
using FplBot.Services.EventHandlers;
using FplBot.Services.WebApi;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Data;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using StackExchange.Redis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Testcontainers.Redis;

namespace FplBot.Tests.E2E;

public class AppFixture : IAsyncLifetime
{
    private static readonly bool ReuseContainers =
        Environment.GetEnvironmentVariable("REUSE_TEST_CONTAINERS") == "true";

    private readonly RedisContainer _redis = new RedisBuilder("redis:latest")
        .WithReuse(ReuseContainers)
        .WithLabel("reuse-id", "app-fixture")
        .Build();
    private WebApplication _app = null!;
    private ConnectionMultiplexer _multiplexer = null!;
    private HttpClient _client = null!;

    public SlackMessageCapture SlackCapture { get; } = new();
    public TokenStore Store { get; private set; } = null!;

    public virtual async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();

        var redisConnStr = _redis.GetConnectionString();
        _multiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnStr + ",allowAdmin=true");
        var redisUrl = $"redis://user:pass@{redisConnStr}";
        var redisOpts = new OptionsWrapper<RedisOptions>(new RedisOptions { REDIS_URL = redisUrl });

        Store = new TokenStore(_multiplexer, redisOpts, NullLogger<TokenStore>.Instance);

        var fakeSlackClient = BuildCapturingSlackClient();
        var fakeSlackClientBuilder = A.Fake<ISlackClientBuilder>();
        A.CallTo(() => fakeSlackClientBuilder.Build(A<string>._)).Returns(fakeSlackClient);

        var globalSettings = JsonSerializer.Deserialize<GlobalSettings>(
            TestResources.Boostrap_Static_Json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var fakeGlobalSettings = GlobalSettingsClientBuilder.Returning(globalSettings);

        var fakeFixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fakeFixtureClient.GetFixtures()).Returns(new List<Fixture>());

        var fakeLeagueClient = A.Fake<ILeagueClient>();
        A.CallTo(() => fakeLeagueClient.GetClassicLeague(A<int>._, A<int>._, A<bool>._))
            .Returns(new ClassicLeague
            {
                Properties = new ClassicLeagueProperties { StartEvent = 1 },
                Standings = new ClassicLeagueStandings { Entries = new List<ClassicLeagueEntry>() }
            });

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["REDIS_URL"] = redisUrl })
            .Build();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Host.UseSerilog((_, lc) => lc.WriteTo.Console());
        builder.Configuration.AddConfiguration(config);

        var active = new List<IFplBotService> { new WebApiService(), new EventHandlersService() };
        FplBotApplication.ConfigureServices(builder.Services, config, _multiplexer, builder.Environment, active,
            cfg => cfg.UsingInMemory((ctx, c) => c.ConfigureEndpoints(ctx)));

        builder.Services.AddSingleton<IConnectionMultiplexer>(_multiplexer);
        builder.Services.AddSingleton(_multiplexer);
        builder.Services.AddStackExchangeRedisCache(o =>
            o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_multiplexer));

        builder.Services.AddSingleton<IGlobalSettingsClient>(fakeGlobalSettings);
        builder.Services.AddSingleton<IFixtureClient>(fakeFixtureClient);
        builder.Services.AddSingleton<ILeagueClient>(fakeLeagueClient);
        builder.Services.AddSingleton<ITransfersClient>(A.Fake<ITransfersClient>());
        builder.Services.AddSingleton<IEntryClient>(A.Fake<IEntryClient>());
        builder.Services.AddSingleton<ILiveClient>(A.Fake<ILiveClient>());
        builder.Services.AddSingleton<IEntryHistoryClient>(A.Fake<IEntryHistoryClient>());
        builder.Services.AddSingleton<IEventStatusClient>(A.Fake<IEventStatusClient>());

        builder.Services.RemoveAll<ISlackClientBuilder>();
        builder.Services.AddSingleton<ISlackClientBuilder>(fakeSlackClientBuilder);

        builder.Services.RemoveAll<IDiscordClient>();
        builder.Services.AddSingleton<IDiscordClient>(A.Fake<IDiscordClient>());

        ConfigureSearchClient(builder.Services);

        _app = builder.Build();
        foreach (var svc in active)
            svc.ConfigureApp(_app);

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public IBus Bus => _app.Services.GetRequiredService<IBus>();
    public IServiceProvider Services => _app.Services;

    public async Task AskSlackbot(SlackTeam team, string input)
    {
        var payload = new
        {
            token = "test",
            team_id = team.TeamId,
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
                channel = team.FplBotSlackChannel,
                ts = "1234567890.123456",
                event_ts = "1234567890.123456"
            }
        };

        var response = await _client.PostAsJsonAsync("/events", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task AskSlackbot(string input) => await AskSlackbot(await SeedTeam(), input);

    public async Task<string> AskDiscord(string commandName, string? optionValue = null, string? subCommandName = null, string? guildId = null, string? channelId = null)
    {
        guildId ??= Guid.NewGuid().ToString("N");
        channelId ??= Guid.NewGuid().ToString("N");

        var data = new JsonObject { ["name"] = commandName, ["type"] = 1 };
        if (optionValue != null)
        {
            var innerOption = new JsonObject { ["name"] = "value", ["value"] = optionValue };
            data["options"] = subCommandName != null
                ? new JsonArray(new JsonObject { ["name"] = subCommandName, ["options"] = new JsonArray(innerOption) })
                : new JsonArray(innerOption);
        }

        var payload = new JsonObject
        {
            ["type"] = 2,
            ["guild_id"] = guildId,
            ["channel_id"] = channelId,
            ["data"] = data
        };

        var response = await _client.PostAsync("/discord/events",
            new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<SlackTeam> SeedTeam(Action<SlackTeam>? configure = null)
    {
        var team = SlackTeamFaker.Generate();
        configure?.Invoke(team);
        await Store.Insert(team);
        return team;
    }

    public async Task<GuildFplSubscription> SeedGuildSubscription(int? leagueId = null, IEnumerable<EventSubscription>? subscriptions = null)
    {
        var sub = new GuildFplSubscription(
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N"),
            leagueId,
            subscriptions ?? []);

        await Services.GetRequiredService<IGuildRepository>().InsertGuildSubscription(sub);
        return sub;
    }

    public async Task SeedSearchEntry(EntryItem entry)
    {
        var options = Services.GetRequiredService<IOptions<SearchOptions>>().Value;
        var client = Services.GetRequiredService<IElasticClient>();
        await client.IndexAsync(entry, i => i.Index(options.EntriesIndex).Id(entry.Id));
        await client.Indices.RefreshAsync(options.EntriesIndex);
    }

    public async Task SeedSearchLeague(LeagueItem league)
    {
        var options = Services.GetRequiredService<IOptions<SearchOptions>>().Value;
        var client = Services.GetRequiredService<IElasticClient>();
        await client.IndexAsync(league, i => i.Index(options.LeaguesIndex).Id(league.Id));
        await client.Indices.RefreshAsync(options.LeaguesIndex);
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

    public virtual async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        _multiplexer?.Dispose();
        if (!ReuseContainers) await _redis.DisposeAsync();
    }

    private ISlackClient BuildCapturingSlackClient()
    {
        var fakeSlackClient = A.Fake<ISlackClient>();

        A.CallTo(() => fakeSlackClient.ChatPostMessage(A<ChatPostMessageRequest>._))
            .ReturnsLazily(call =>
            {
                SlackCapture.Record(call.Arguments.Get<ChatPostMessageRequest>(0)!);
                return Task.FromResult(new ChatPostMessageResponse { Ok = true, ts = "ts123" });
            });

        A.CallTo(() => fakeSlackClient.ChatPostMessage(A<string>._, A<string>._))
            .ReturnsLazily(call =>
            {
                SlackCapture.Record(new ChatPostMessageRequest
                {
                    Channel = call.Arguments.Get<string>(0),
                    Text = call.Arguments.Get<string>(1)
                });
                return Task.FromResult(new ChatPostMessageResponse { Ok = true, ts = "ts123" });
            });

        A.CallTo(() => fakeSlackClient.UsersList())
            .Returns(Task.FromResult(new UsersListResponse { Ok = true, Members = [] }));

        return fakeSlackClient;
    }
}

[CollectionDefinition("App")]
public class AppCollection : ICollectionFixture<AppFixture>;
