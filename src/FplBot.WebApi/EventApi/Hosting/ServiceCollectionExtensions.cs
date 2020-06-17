using Microsoft.Extensions.DependencyInjection;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public static class ServiceCollectionExtensions
    {
        public static ISlackbotEventHandlersBuilder AddSlackBotEventHandlers(this IServiceCollection services)
        {
            services.AddSingleton<ISelectEventHandlers, EventHandlerSelector>();
            return new SlackBotEventHandlersBuilder(services);
        }
    }
}