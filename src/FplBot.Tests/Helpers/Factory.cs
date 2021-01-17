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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Xunit.Abstractions;

namespace FplBot.Tests.Helpers
{
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
            services.AddDistributedFplBot(configuration)
                .AddFplBotEventHandlers();

            SlackClient = A.Fake<ISlackClient>();
            GameweekClient = A.Fake<IGameweekClient>();
            var boostrapStaticPrGw_2020_11_Gw9_GwFinished = JsonConvert.DeserializeObject<GlobalSettings>(TestResources.Boostrap_Static_Json);
            A.CallTo(() => GameweekClient.GetGameweeks()).Returns(boostrapStaticPrGw_2020_11_Gw9_GwFinished.Gameweeks);
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(boostrapStaticPrGw_2020_11_Gw9_GwFinished.Players);
            var teamsClient = A.Fake<ITeamsClient>();
            A.CallTo(() => teamsClient.GetAllTeams()).Returns(boostrapStaticPrGw_2020_11_Gw9_GwFinished.Teams);
            var globalClient = A.Fake<IGlobalSettingsClient>();
            A.CallTo(() => globalClient.GetGlobalSettings()).Returns(boostrapStaticPrGw_2020_11_Gw9_GwFinished);
            var elasticClient = A.Fake<IElasticClient>();
            
            var slackClientServiceMock = A.Fake<ISlackClientBuilder>();
            A.CallTo(() => slackClientServiceMock.Build(A<string>.Ignored)).Returns(SlackClient);
            
            services.Replace<ISlackClientBuilder>(slackClientServiceMock);
            services.Replace<IGameweekClient>(GameweekClient);
            services.Replace<IPlayerClient>(playerClient);
            services.Replace<ITeamsClient>(teamsClient);
            services.Replace<IGlobalSettingsClient>(globalClient);
            services.Replace<ITokenStore>(new DontCareRepo());
            services.Replace<ISlackTeamRepository>(new InMemorySlackTeamRepository(new OptionsWrapper<FplbotOptions>(new FplbotOptions())));
            services.Replace<IElasticClient>(elasticClient);
            services.AddSingleton<ILogger<CookieFetcher>, XUnitTestOutputLogger<CookieFetcher>>(s => new XUnitTestOutputLogger<CookieFetcher>(logger));
            services.AddSingleton<IMessageSession>(A.Fake<IMessageSession>()); // Faking NServicebus
            var provider = services.BuildServiceProvider();
            return provider;
        }
        
        public static ISlackClient SlackClient { get; set; }
        public static IGameweekClient GameweekClient { get; set; }
       

     
        
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
        public Task<IEnumerable<string>> GetTokens()
        {
            return Task.FromResult(new List<string>().AsEnumerable());
        }

        public Task<string> GetTokenByTeamId(string teamId)
        {
            return Task.FromResult(string.Empty);
        }

        public Task Delete(string token)
        {
            return Task.CompletedTask;
        }
    }
}