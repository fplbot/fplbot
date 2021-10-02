using System;
using Microsoft.Extensions.DependencyInjection;

namespace Discord.Net.Endpoints.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IDiscordbotEventsBuilder AddDiscordBotEvents<T>(this IServiceCollection services) where T: class, IGuildStore
        {
            services.AddSingleton<IGuildStore,T>();
            return new DiscordbotEventsBuilder(services);
        }

        public static IServiceCollection AddDiscordBotDistribution(this IServiceCollection services, Action<DiscordOAuthOptions> action)
        {
            services.Configure<DiscordOAuthOptions>(action);
            return services;
        }
    }
}
