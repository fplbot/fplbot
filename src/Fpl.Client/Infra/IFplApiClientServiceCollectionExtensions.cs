using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fpl.Client.Clients
{
    public static class IFplApiClientServiceCollectionExtensions
    {
        public static IServiceCollection AddFplApiClient(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient<IFplClient, FplClient>();
            services.ConfigureOptions<FplClientOptionsConfigurator>();
            services.AddSingleton<Authenticator>();
            services.AddSingleton<CookieFetcher>();
            services.AddDistributedMemoryCache();
            services.AddSingleton<CookieCache>();
            services.AddSingleton<FplHttpHandler>();
            services.Configure<FplApiClientOptions>(config);
            return services;
        }
    }
}