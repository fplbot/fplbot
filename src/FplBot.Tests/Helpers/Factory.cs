using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Xunit.Abstractions;

namespace FplBot.Tests.Helpers
{
    public static class Factory
    {
        public static T Create<T>(ITestOutputHelper logger = null)
        {
            return BuildServiceProvider(logger).GetService<T>();
        }

        public static IHandleAppMentions GetHandler<T>(ITestOutputHelper logger)
        {
            var allHandlers = BuildServiceProvider(logger).GetServices<IHandleAppMentions>();
            return allHandlers.First(h => h is T);
        }
        
        private static ServiceProvider BuildServiceProvider(ITestOutputHelper logger)
        {
            var config = new ConfigurationBuilder();
            config.AddJsonFile("appsettings.json", optional: true);
            config.AddJsonFile("appsettings.Local.json", optional: true);
            config.AddEnvironmentVariables();
            var configuration = config.Build();

            var services = new ServiceCollection();
            services.AddDistributedFplBot<InMemorySlackTeamRepository>(configuration.GetSection("fpl"))
                .AddFplBotEventHandlers<DontCareRepo>();

            SlackClient = A.Fake<ISlackClient>();
            GameweekClient = A.Fake<IGameweekClient>();
            A.CallTo(() => GameweekClient.GetGameweeks()).Returns(new List<Gameweek>
            {
                new Gameweek
                {
                    Id = 1,
                    IsCurrent = false
                },
                new Gameweek
                {
                    Id = 2,
                    IsCurrent = true
                },
                new Gameweek
                {
                    Id = 3,
                    IsNext = true
                }
            });
            
            var slackClientServiceMock = A.Fake<ISlackClientBuilder>();
            A.CallTo(() => slackClientServiceMock.Build(A<string>.Ignored)).Returns(SlackClient);
            services.Replace<ISlackClientBuilder>(slackClientServiceMock);
            services.Replace<IGameweekClient>(GameweekClient);

            services.AddSingleton<ILogger<CookieFetcher>, XUnitTestOutputLogger<CookieFetcher>>(s => new XUnitTestOutputLogger<CookieFetcher>(logger));
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