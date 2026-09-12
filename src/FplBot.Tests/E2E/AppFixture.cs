using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Services.EventHandlers;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Data;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Testcontainers.Redis;

namespace FplBot.Tests.E2E;

// The one shared app slice for tests: composes both the EventHandlers service's DI graph
// (real in-memory MassTransit bus + real consumers) and the WebApi service's DI graph
// (real Slack app-mention dispatch via ISelectAppMentionEventHandlers), so a test can go in
// through either a real webhook-dispatch entry point or a published event, and come out
// through a real consumer, asserting on the final captured Slack message either way.
public class AppFixture : IAsyncLifetime
{
    // Local-only: set REUSE_TEST_CONTAINERS=true to keep this container warm across
    // `dotnet test` runs instead of tearing it down each time. Never set in CI.
    private static readonly bool ReuseContainers =
        Environment.GetEnvironmentVariable("REUSE_TEST_CONTAINERS") == "true";

    private readonly RedisContainer _redis = new RedisBuilder("redis:latest")
        .WithReuse(ReuseContainers)
        .WithLabel("reuse-id", "app-fixture")
        .Build();
    private IHost _host = null!;
    private ConnectionMultiplexer _multiplexer = null!;

    public SlackMessageCapture SlackCapture { get; } = new();
    public TokenStore Store { get; private set; } = null!;

    public async ValueTask InitializeAsync()
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

        // appsettings.json (copied from FplBot's own project) carries real dev-default values —
        // including "search", DiscordAppId, DISCORD_TOKEN — that AddFplBotSlackWebEndpoints'
        // startup validation needs. Only REDIS_URL needs overriding, to point at this test's
        // ephemeral container instead of devenv's fixed instance.
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["REDIS_URL"] = redisUrl })
            .Build();

        _host = Host.CreateDefaultBuilder()
            .UseEnvironment("Testing")
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IConnectionMultiplexer>(_multiplexer);
                services.AddSingleton(_multiplexer);
                // AppMentionEventHandlerSelector (Slackbot.Net.Endpoints) resolves handlers via
                // IHttpContextAccessor.HttpContext.RequestServices, not an injected IServiceProvider
                // — it expects to run inside a real ASP.NET Core request. We give it a synthetic
                // HttpContext after the host starts (see below) so it can be called directly in
                // tests without a real HTTP transport.
                services.AddHttpContextAccessor();
                services.AddStackExchangeRedisCache(o =>
                    o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_multiplexer));

                // EventHandlers service (real MassTransit consumers) and WebApi service (real
                // Slack app-mention dispatch) composed into one test host.
                var hostEnvironment = new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Testing" };
                new EventHandlersService().Configure(services, config, _multiplexer, hostEnvironment);
                services.AddFplBotSlackWebEndpoints(config, _multiplexer, hostEnvironment);

                // FPL API fakes — registered after both Configure() and AddFplBotSlackWebEndpoints()
                // so they win the last-registration-wins resolution for constructor injection.
                services.AddSingleton<IGlobalSettingsClient>(fakeGlobalSettings);
                services.AddSingleton<IFixtureClient>(fakeFixtureClient);
                services.AddSingleton<ILeagueClient>(fakeLeagueClient);
                services.AddSingleton<ITransfersClient>(A.Fake<ITransfersClient>());
                services.AddSingleton<IEntryClient>(A.Fake<IEntryClient>());
                services.AddSingleton<ILiveClient>(A.Fake<ILiveClient>());
                services.AddSingleton<IEntryHistoryClient>(A.Fake<IEntryHistoryClient>());
                services.AddSingleton<IEventStatusClient>(A.Fake<IEventStatusClient>());

                // Replace the real ISlackClientBuilder with the capturing fake — must come after
                // both Configure() and AddFplBotSlackWebEndpoints(), which both register a real one.
                // RemoveAll clears every descriptor registered by either call.
                services.RemoveAll<ISlackClientBuilder>();
                services.AddSingleton<ISlackClientBuilder>(fakeSlackClientBuilder);

                // Formatting helpers used by GameweekStarted/Finished handlers — also registered
                // by both calls above; re-registering here is redundant but harmless.
                services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
                services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
                services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
                services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();

                services.AddMassTransit(x =>
                {
                    new EventHandlersService().ConfigureMassTransit(x);
                    x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
                });
            })
            .Build();

        await _host.StartAsync();
    }

    public IBus Bus => _host.Services.GetRequiredService<IBus>();
    public IServiceProvider Services => _host.Services;

    // AppMentionEventHandlerSelector (Slackbot.Net.Endpoints) resolves handlers via
    // IHttpContextAccessor.HttpContext.RequestServices, expecting to run inside a real ASP.NET
    // Core request. IHttpContextAccessor is AsyncLocal-backed, so the HttpContext has to be set
    // within the same async flow as the dispatch call, not once during fixture setup — hence
    // this does both together rather than exposing the raw selector.
    public async Task<EventHandledResponse> DispatchAppMention(EventMetaData meta, AppMentionEvent slackEvent)
    {
        _host.Services.GetRequiredService<IHttpContextAccessor>().HttpContext =
            new DefaultHttpContext { RequestServices = _host.Services };

        var selector = _host.Services.GetRequiredService<ISelectAppMentionEventHandlers>();
        var handlers = await selector.GetAppMentionEventHandlerFor(meta, slackEvent);
        return await handlers.Single().Handle(meta, slackEvent);
    }

    public async Task FlushRedisAsync()
    {
        var server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());
        await server.FlushAllDatabasesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
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
