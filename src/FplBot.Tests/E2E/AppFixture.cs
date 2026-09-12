using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Data.Slack;
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

public class AppFixture : IAsyncLifetime
{
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
                services.AddHttpContextAccessor();
                services.AddStackExchangeRedisCache(o =>
                    o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_multiplexer));

                var hostEnvironment = new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Testing" };
                new EventHandlersService().Configure(services, config, _multiplexer, hostEnvironment);
                services.AddFplBotSlackWebEndpoints(config, _multiplexer, hostEnvironment);

                services.AddSingleton<IGlobalSettingsClient>(fakeGlobalSettings);
                services.AddSingleton<IFixtureClient>(fakeFixtureClient);
                services.AddSingleton<ILeagueClient>(fakeLeagueClient);
                services.AddSingleton<ITransfersClient>(A.Fake<ITransfersClient>());
                services.AddSingleton<IEntryClient>(A.Fake<IEntryClient>());
                services.AddSingleton<ILiveClient>(A.Fake<ILiveClient>());
                services.AddSingleton<IEntryHistoryClient>(A.Fake<IEntryHistoryClient>());
                services.AddSingleton<IEventStatusClient>(A.Fake<IEventStatusClient>());

                services.RemoveAll<ISlackClientBuilder>();
                services.AddSingleton<ISlackClientBuilder>(fakeSlackClientBuilder);

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

    public async Task<EventHandledResponse> DispatchAppMention(EventMetaData meta, AppMentionEvent slackEvent)
    {
        _host.Services.GetRequiredService<IHttpContextAccessor>().HttpContext =
            new DefaultHttpContext { RequestServices = _host.Services };

        var selector = _host.Services.GetRequiredService<ISelectAppMentionEventHandlers>();
        var handlers = await selector.GetAppMentionEventHandlerFor(meta, slackEvent);
        return await handlers.Single().Handle(meta, slackEvent);
    }

    public async Task<SlackTeam> SeedTeam(Action<SlackTeam>? configure = null)
    {
        var team = SlackTeamFaker.Generate();
        configure?.Invoke(team);
        await Store.Insert(team);
        return team;
    }

    public async Task<string> AskSlackbot(SlackTeam team, string input)
    {
        var dummy = Factory.CreateDummyEvent(team, input);
        var response = await DispatchAppMention(dummy.meta, dummy.@event);
        return response.Response;
    }

    public async Task<string> AskSlackbot(string input) => await AskSlackbot(await SeedTeam(), input);

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
