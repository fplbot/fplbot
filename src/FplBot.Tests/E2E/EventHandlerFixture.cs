using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Services.EventHandlers;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Data;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Redis;

namespace FplBot.Tests.E2E;

public class EventHandlerFixture : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:latest").Build();
    private IHost _host = null!;
    private ConnectionMultiplexer _multiplexer = null!;

    public SlackMessageCapture SlackCapture { get; } = new();
    public TokenStore Store { get; private set; } = null!;

    public async Task InitializeAsync()
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
        var fakeGlobalSettings = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => fakeGlobalSettings.GetGlobalSettings()).Returns(globalSettings);

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
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["REDIS_URL"] = redisUrl,
                ["DiscordAppId"] = "test",
                ["DISCORD_TOKEN"] = "test",
            })
            .Build();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IConnectionMultiplexer>(_multiplexer);
                services.AddSingleton(_multiplexer);
                services.AddStackExchangeRedisCache(o =>
                    o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_multiplexer));

                // FPL API fakes — registered before the formatting helpers that depend on them
                services.AddSingleton<IGlobalSettingsClient>(fakeGlobalSettings);
                services.AddSingleton<IFixtureClient>(fakeFixtureClient);
                services.AddSingleton<ILeagueClient>(fakeLeagueClient);
                services.AddSingleton<ITransfersClient>(A.Fake<ITransfersClient>());
                services.AddSingleton<IEntryClient>(A.Fake<IEntryClient>());
                services.AddSingleton<ILiveClient>(A.Fake<ILiveClient>());
                services.AddSingleton<IEntryHistoryClient>(A.Fake<IEntryHistoryClient>());
                services.AddSingleton<IEventStatusClient>(A.Fake<IEventStatusClient>());

                // Discord and Slack services (EventHandlersService.Configure calls AddSlackServices internally)
                new EventHandlersService().Configure(services, config, _multiplexer, null!);

                // Replace the real ISlackClientBuilder with the capturing fake — must come AFTER Configure()
                // because Configure() calls AddSlackServices() which re-registers the real builder.
                // Use RemoveAll to clear every descriptor (there can be more than one from multiple AddSlackServices calls).
                services.RemoveAll<ISlackClientBuilder>();
                services.AddSingleton<ISlackClientBuilder>(fakeSlackClientBuilder);

                // Formatting helpers used by GameweekStarted/Finished handlers
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

    public async Task FlushRedisAsync()
    {
        var server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());
        await server.FlushAllDatabasesAsync();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        _multiplexer?.Dispose();
        await _redis.DisposeAsync();
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
            .Returns(Task.FromResult(new UsersListResponse { Ok = true, Members = Array.Empty<User>() }));

        return fakeSlackClient;
    }
}
