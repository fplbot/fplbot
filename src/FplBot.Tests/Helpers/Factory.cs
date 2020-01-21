using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Fpl.Client.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Connections;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using Xunit.Abstractions;

namespace FplBot.Tests.Helpers
{
    public static class Factory
    {
        public static T Create<T>(ITestOutputHelper logger = null)
        {
            return BuildServiceProvider(logger).GetService<T>();
        }

        public static IHandleMessages GetHandler<T>(ITestOutputHelper logger)
        {
            var allHandlers = BuildServiceProvider(logger).GetServices<IHandleMessages>();
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
            var configurationSection = configuration.GetSection("fpl");
            services.AddSlackbotWorker(configuration).AddFplBot(o =>
            {
                o.Login = configurationSection["Login"];
                o.Password = configurationSection["Password"];
            });

            services.ReplacePublishersWithDebugPublisher(logger);
            var getConnectionDetails = A.Fake<IGetConnectionDetails>();
            A.CallTo(() => getConnectionDetails.GetConnectionBotDetails()).Returns(new BotDetails {Id = "UREFQD887", Name = "fplbot"});
            services.Replace<IGetConnectionDetails>(getConnectionDetails);
            SlackClient = A.Fake<ISlackClient>();
            
            services.Replace<ISlackClient>(SlackClient);

            services.AddSingleton<ILogger<CookieFetcher>, XUnitTestOutputLogger<CookieFetcher>>(s => new XUnitTestOutputLogger<CookieFetcher>(logger));
            var provider = services.BuildServiceProvider();
            return provider;
        }
        
        public static ISlackClient SlackClient { get; set; }
        
        // remove live slack integration and replace with debugging publishers
        private static void ReplacePublishersWithDebugPublisher(this ServiceCollection services, ITestOutputHelper logger)
        {
            var serviceDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(IPublisher)).ToList();
            foreach (var service in serviceDescriptors)
            {
                var t = services.Remove(service);
            }

            services.AddSingleton<IPublisher, XUnitTestoutPublisher>(s => new XUnitTestoutPublisher(logger));
        }
        
        private static void Replace<T>(this ServiceCollection services, T replacement) where T : class
        {
            var serviceDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(T)).ToList();
            foreach (var service in serviceDescriptors)
            {
                var t = services.Remove(service);
            }

            services.AddSingleton<T>(s => replacement);
        }
    }
}