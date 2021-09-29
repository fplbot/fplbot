using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.OAuth;

namespace Slackbot.Net.Endpoints.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static ISlackbotHandlersBuilder AddSlackBotEvents(this IServiceCollection services)
        {
            services.AddSingleton<ISelectAppMentionEventHandlers, AppMentionEventHandlerSelector>();
            return new SlackBotHandlersBuilder(services);
        }

        public static ISlackbotHandlersBuilder AddSlackBotEvents<T>(this IServiceCollection services) where T: class, ITokenStore
        {
            services.AddSingleton<ITokenStore, T>();
            return services.AddSlackBotEvents();
        }

        public static IServiceCollection AddSlackbotDistribution(this IServiceCollection services, Action<OAuthOptions> action)
        {
            services.Configure<OAuthOptions>(action);
            services.AddHttpClient<OAuthClient>((s, c) =>
            {
                c.BaseAddress = new Uri("https://slack.com/api/");
                c.Timeout = TimeSpan.FromSeconds(15);
            });
            return services;
        }
    }

    public class OAuthOptions
    {
        public OAuthOptions()
        {
            OnSuccess = (_,_,_) => Task.CompletedTask;
        }
        public string CLIENT_ID { get; set; }
        public string CLIENT_SECRET { get; set; }
        public string SuccessRedirectUri { get; set; } = "/success?default=1";
        public Func<string, string, IServiceProvider, Task> OnSuccess { get; set; }
    }
}
