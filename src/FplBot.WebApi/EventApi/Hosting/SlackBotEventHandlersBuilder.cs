using Microsoft.Extensions.DependencyInjection;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public class SlackBotEventHandlersBuilder : ISlackbotEventHandlersBuilder
    {
        private readonly IServiceCollection _services;

        public SlackBotEventHandlersBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void AddEventHandler<T>() where T : class, IHandleEvent
        {
            _services.AddSingleton<IHandleEvent, T>();
        }
    }
}