using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Clients;
using Fpl.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using FplBot.Data.Slack;
using Nest;

using NServiceBus;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FplBot.Tests.Helpers;

public static class Factory
{
    public static T Create<T>(ITestOutputHelper logger = null)
    {
        return BuildServiceProvider(logger).GetService<T>();
    }

    public static IEnumerable<IHandleAppMentions> GetAllHandlers(ITestOutputHelper logger)
    {
        return BuildServiceProvider(logger).GetServices<IHandleAppMentions>();
    }

    public static IHandleAppMentions GetHandler<T>(ITestOutputHelper logger)
    {
        return GetAllHandlers(logger).First(h => h is T);
    }

    private static ServiceProvider BuildServiceProvider(ITestOutputHelper logger)
    {
        var config = new ConfigurationBuilder();
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddJsonFile("appsettings.Local.json", optional: true);
        config.AddEnvironmentVariables();
        var configuration = config.Build();

        var services = new ServiceCollection();
        services.AddFplBotSlackWebEndpoints(configuration, A.Fake<IConnectionMultiplexer>());
        services.AddDistributedMemoryCache();

        SlackClient = A.Fake<ISlackClient>();

        var boostrapStaticPrGw_2020_11_Gw9_GwFinished = JsonSerializer.Deserialize<GlobalSettings>(TestResources.Boostrap_Static_Json, new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var globalClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => globalClient.GetGlobalSettings()).Returns(boostrapStaticPrGw_2020_11_Gw9_GwFinished);
        var elasticClient = A.Fake<IElasticClient>();

        var slackClientServiceMock = A.Fake<ISlackClientBuilder>();
        A.CallTo(() => slackClientServiceMock.Build(A<string>.Ignored)).Returns(SlackClient);

        services.Replace<ISlackClientBuilder>(slackClientServiceMock);
        services.Replace<IGlobalSettingsClient>(globalClient);
        services.Replace<ITokenStore>(new DontCareRepo());
        services.Replace<ISlackTeamRepository>(new InMemorySlackTeamRepository());
        services.Replace<IElasticClient>(elasticClient);
        services.AddSingleton<ILogger<CookieFetcher>, XUnitTestOutputLogger<CookieFetcher>>(s => new XUnitTestOutputLogger<CookieFetcher>(logger));
        services.AddSingleton<IMessageSession>(A.Fake<IMessageSession>()); // Faking NServicebus
        services.AddFplWorkers();
        var provider = services.BuildServiceProvider();
        return provider;
    }

    public static ISlackClient SlackClient { get; set; }

    private static void Replace<T>(this ServiceCollection services, T replacement) where T : class
    {
        var serviceDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(T)).ToList();
        foreach (var service in serviceDescriptors)
        {
            var t = services.Remove(service);
        }

        services.AddSingleton<T>(s => replacement);
    }

    public static (EventMetaData meta, AppMentionEvent @event) CreateDummyEvent(string input)
    {
        return (new EventMetaData
            {
                Team_Id =  "123",
            },
            new AppMentionEvent()
            {
                Text = input
            });
    }

    public static (EventMetaData meta, AppMentionEvent @event) CreateDummyEventByUser(string input, string userId)
    {
        return (new EventMetaData
            {
                Team_Id =  "123",
            },
            new AppMentionEvent()
            {
                Text = input,
                User = userId,
            });
    }
}

internal class DontCareRepo : ITokenStore
{
    public Task<Workspace> Delete(string token)
    {
        return Task.FromResult<Workspace>(null);
    }

    public Task Insert(Workspace slackTeam)
    {
        return Task.CompletedTask;
    }
}
