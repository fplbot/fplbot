using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Workers.Publishers.Logger;
using Slackbot.Net.Workers.Publishers.Slack;

namespace FplBot.ConsoleApps.Clients
{
    public static class IFplBotServiceCollectionExtensions
    {
        public static IServiceCollection AddFplBot(this IServiceCollection services, IConfiguration config)
        {
            services.AddSlackbotWorker(config)
                .AddPublisher<SlackPublisher>()
                .AddPublisher<LoggerPublisher>()
                .AddHandler<FplPlayerCommandHandler>()
                .AddHandler<FplCommandHandler>();
            return services;
        }
    }
}