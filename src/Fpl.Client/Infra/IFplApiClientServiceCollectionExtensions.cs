using Fpl.Client.Abstractions;
using Fpl.Client.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fpl.Client.Infra
{
    public static class IFplApiClientServiceCollectionExtensions
    {
        public static IServiceCollection AddFplApiClient(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient<IEntryClient, EntryClient>();
            services.AddHttpClient<IEntryHistoryClient, EntryHistoryClient>();
            services.AddHttpClient<IFixtureClient, FixtureClient>();
            services.AddHttpClient<IGameweekClient, GameweekClient>();
            services.AddHttpClient<IGlobalSettingsClient, GlobalSettingsClient>();
            services.AddHttpClient<ILeagueClient, LeagueClient>();
            services.AddHttpClient<IPlayerClient, PlayerClient>();
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