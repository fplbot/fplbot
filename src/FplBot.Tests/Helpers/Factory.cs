using System.Text.Json;
using System.Text.Json.Serialization;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using FplBot.Data.Slack;
using FplBot.WebApi.Slack.Data;
using MassTransit;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FplBot.Tests.Helpers;

public static class Factory
{
    // One real Redis container, lazily started on first use and shared for the whole test run
    // (mirrors EventHandlerFixture's pattern, just scoped to the process instead of one test class).
    private static readonly Lazy<RedisContainer> RedisContainerInstance = new(() =>
    {
        var container = new RedisBuilder("redis:latest").Build();
        container.StartAsync().GetAwaiter().GetResult();
        return container;
    });

    private static readonly Lazy<string> RedisUrl = new(() => $"redis://user:pass@{RedisContainerInstance.Value.GetConnectionString()}");

    // Exposed so other test classes can point real (non-faked) dependents at the same shared
    // Redis instance instead of spinning up their own container just to reach a healthy one.
    public static readonly Lazy<IConnectionMultiplexer> RedisConnection = new(() =>
        ConnectionMultiplexer.Connect(RedisContainerInstance.Value.GetConnectionString() + ",allowAdmin=true"));

    public static (T Instance, SlackTeam Team) Create<T>(ITestOutputHelper? logger = null) where T : notnull
    {
        var provider = BuildServiceProvider(logger, out var team);
        return (provider.GetRequiredService<T>(), team);
    }

    public static (IEnumerable<IHandleAppMentions> Handlers, SlackTeam Team) GetAllHandlers(ITestOutputHelper logger)
    {
        var provider = BuildServiceProvider(logger, out var team);
        return (provider.GetServices<IHandleAppMentions>(), team);
    }

    public static (IHandleAppMentions Handler, SlackTeam Team) GetHandler<T>(ITestOutputHelper logger)
    {
        var (handlers, team) = GetAllHandlers(logger);
        return (handlers.First(h => h is T), team);
    }

    private static ServiceProvider BuildServiceProvider(ITestOutputHelper? logger, out SlackTeam team)
    {
        var config = new ConfigurationBuilder();
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddJsonFile("appsettings.Local.json", optional: true);
        config.AddEnvironmentVariables();
        config.AddInMemoryCollection(new Dictionary<string, string?> { ["REDIS_URL"] = RedisUrl.Value });
        var configuration = config.Build();

        var services = new ServiceCollection();
        var hostEnvironment = new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Testing" };
        services.AddFplBotSlackWebEndpoints(configuration, RedisConnection.Value, hostEnvironment);
        services.AddDistributedMemoryCache();

        SlackClient = A.Fake<ISlackClient>();
        services.Replace<IPublishEndpoint>(PublishEndpoint = new TestPublishEndpoint());

        // Real FPL API clients otherwise hit the live fantasy.premierleague.com API on every
        // Handle() call — slow (multi-second) and non-deterministic. Fake GetGlobalSettings()
        // from a real, locally-embedded bootstrap-static snapshot instead (mirrors EventHandlerFixture).
        var globalSettings = JsonSerializer.Deserialize<GlobalSettings>(
            TestResources.Boostrap_Static_Json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        services.Replace<IGlobalSettingsClient>(GlobalSettingsClientBuilder.Returning(globalSettings));

        services.AddSingleton(hostEnvironment);
        services.AddFplWorkers();
        var provider = services.BuildServiceProvider();

        // Seed a uniquely-identified team per call so concurrently-running tests never collide
        // on the same Redis key, even though they all share one real container.
        team = SlackTeamFaker.Generate();
        new TokenStore(RedisConnection.Value, Options.Create(new RedisOptions { REDIS_URL = RedisUrl.Value }), NullLogger<TokenStore>.Instance)
            .Insert(team).GetAwaiter().GetResult();

        return provider;
    }

    public static ISlackClient SlackClient { get; set; } = null!;
    public static TestPublishEndpoint PublishEndpoint { get; set; } = null!;

    private static void Replace<T>(this ServiceCollection services, T replacement) where T : class
    {
        var serviceDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(T)).ToList();
        foreach (var service in serviceDescriptors)
        {
            var t = services.Remove(service);
        }

        services.AddSingleton<T>(s => replacement);
    }

    public static (EventMetaData meta, AppMentionEvent @event) CreateDummyEvent(SlackTeam team, string input)
    {
        return (new EventMetaData
            {
                Team_Id = team.TeamId,
            },
            new AppMentionEvent()
            {
                Text = input,
                Channel = team.FplBotSlackChannel
            });
    }

    public static (EventMetaData meta, AppMentionEvent @event) CreateDummyEventByUser(SlackTeam team, string input, string userId)
    {
        return (new EventMetaData
            {
                Team_Id = team.TeamId,
            },
            new AppMentionEvent()
            {
                Text = input,
                Channel = team.FplBotSlackChannel,
                User = userId,
            });
    }
}
