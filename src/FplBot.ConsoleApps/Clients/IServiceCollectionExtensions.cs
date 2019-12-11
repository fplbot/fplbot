using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
    

namespace FplBot.ConsoleApps.Clients
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddFplApiClient(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient<IFplClient, FplClient>();
            services.ConfigureOptions<FplClientOptionsConfigurator>();
            services.AddSingleton<CookieFetcher>();
            services.AddDistributedMemoryCache();
            services.AddSingleton<CookieCache>();
            services.AddSingleton<FplHttpHandler>();
            services.Configure<FplApiClientOptions>(config);
            services.Decorate<IFplClient, TryCatchFplClient>();
            return services;
        }
    }
}