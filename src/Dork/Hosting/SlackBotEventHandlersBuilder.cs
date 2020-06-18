using Dork.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dork.Hosting
{
    public class SlackBotEventHandlersBuilder : ISlackbotEventHandlersBuilder
    {
        private readonly IServiceCollection _services;

        public SlackBotEventHandlersBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public ISlackbotEventHandlersBuilder AddHandler<T>() where T : class, IHandleEvent
        {
            _services.AddSingleton<IHandleEvent, T>();
            return this;
        }
    }
}