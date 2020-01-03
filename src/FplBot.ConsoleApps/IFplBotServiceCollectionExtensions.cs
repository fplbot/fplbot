using FplBot.ConsoleApps.Handlers;
using FplBot.ConsoleApps.RecurringActions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.Publishers.Logger;
using Slackbot.Net.Extensions.Publishers.Slack;

namespace FplBot.ConsoleApps
{
    public static class IFplBotServiceCollectionExtensions
    {
        public static IServiceCollection AddFplBot(this IServiceCollection services, IConfiguration config)
        {
            services.AddSlackbotWorker(config)
                .AddPublisher<SlackPublisher>()
                .AddPublisher<LoggerPublisher>()
                .AddHandler<FplPlayerCommandHandler>()
                .AddHandler<FplCommandHandler>()
                .AddHandler<FplNextGameweekCommandHandler>()
                .AddHandler<FplInjuryCommandHandler>()
                .AddHandler<FplCaptainCommandHandler>()
                
                .AddRecurring<NextGameweekRecurringAction>()
                .BuildRecurrers();
            
            services.Configure<FplbotOptions>(config);

            return services;
        }
    }
}