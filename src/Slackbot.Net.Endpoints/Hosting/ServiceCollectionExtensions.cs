using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Endpoints.Abstractions;

namespace Slackbot.Net.Endpoints.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static ISlackbotEventHandlersBuilder AddSlackBotEventHandlers(this IServiceCollection services)
        {
            services.AddSingleton<ISelectEventHandlers, EventHandlerSelector>();
            services.AddSingleton<ISlackClientService, SlackClientService>();
            return new SlackBotEventHandlersBuilder(services);
        }
    }
}