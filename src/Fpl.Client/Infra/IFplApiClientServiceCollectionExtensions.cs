using Fpl.Client.Abstractions;
using Fpl.Client.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Fpl.Client.Infra
{
    public static class IFplApiClientServiceCollectionExtensions
    {
        public static IServiceCollection AddFplApiClient(this IServiceCollection services, IConfiguration config)
        {
            AddFplApiClient(services);
            services.Configure<FplApiClientOptions>(config);
            return services;
        }
        
        public static IServiceCollection AddFplApiClient(this IServiceCollection services, Action<FplApiClientOptions> configurator)
        {
            AddFplApiClient(services);
            services.Configure<FplApiClientOptions>(configurator);
            return services;
        }

        private static void AddFplApiClient(IServiceCollection services)
        {
            services.AddTransient<FplDelegatingHandler>();
            services.AddHttpClient<ICachedHttpClient, CachedHttpClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<IEntryClient, EntryClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<IEntryHistoryClient, EntryHistoryClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<ILeagueClient, LeagueClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<ITransfersClient, TransfersClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddSingleton<IFixtureClient, FixtureClient>();
            services.AddSingleton<IGameweekClient, GameweekClient>();
            services.AddSingleton<IPlayerClient, PlayerClient>();
            services.AddSingleton<ITeamsClient, TeamsClient>();
            services.AddSingleton<ILiveClient, LiveClient>();
            services.AddSingleton<IGlobalSettingsClient, GlobalSettingsClient>();
            services.ConfigureOptions<FplClientOptionsConfigurator>();
            services.AddSingleton<Authenticator>();
            services.AddSingleton<CookieFetcher>();
            services.AddDistributedMemoryCache();
            services.AddSingleton<CookieCache>();
            services.AddSingleton<FplHttpHandler>();
        }
    }
}