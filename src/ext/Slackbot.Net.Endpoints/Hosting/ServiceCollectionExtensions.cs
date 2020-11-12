using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Abstractions;

namespace Slackbot.Net.Endpoints.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static ISlackbotHandlersBuilder AddSlackBotEvents<T>(this IServiceCollection services) where T: class, ITokenStore
        {
            services.AddSingleton<ITokenStore, T>();
            services.AddSingleton<ISelectAppMentionEventHandlers, AppMentionEventHandlerSelector>();
            return new SlackBotHandlersBuilder(services);
        }
    }
}