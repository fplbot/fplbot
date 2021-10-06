using Fpl.Client.Abstractions;
using Fpl.Client.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using StackExchange.Redis;

namespace Fpl.Client.Infra
{
    public static class IFplApiClientServiceCollectionExtensions
    {
        public static IServiceCollection AddFplApiClient(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<FplApiClientOptions>(config.GetSection("fpl"));
            AddFplApiClientBase(services, config);
            return services;
        }

        private static void AddFplApiClientBase(IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<FplDelegatingHandler>();
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddHttpClient<IEntryClient, EntryClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<IEntryHistoryClient, EntryHistoryClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<IFixtureClient, FixtureClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<ILeagueClient, LeagueClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<ITransfersClient, TransfersClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<IGlobalSettingsClient, GlobalSettingsClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<ILiveClient, LiveClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.AddHttpClient<IEventStatusClient, EventStatusClient>().AddHttpMessageHandler<FplDelegatingHandler>();
            services.ConfigureOptions<FplClientOptionsConfigurator>();
            services.AddSingleton<Authenticator>();
            services.AddSingleton<CookieFetcher>();
            services.AddSingleton<CookieCache>();
            services.AddSingleton<FplHttpHandler>();
        }
    }

    internal class HerokuRedisOptions
    {
        public string REDIS_URL { get; set; } // Set by Heroku
        public string GetRedisPassword => RedisUri().UserInfo.Split(":")[1];

        public string GetRedisUsername => RedisUri().UserInfo.Split(":")[0];

        public string GetRedisServerHostAndPort => REDIS_URL.Split("@")[1];

        private Uri _uri;

        private Uri RedisUri()
        {
            if (_uri == null)
            {
                _uri = new Uri(REDIS_URL);
            }
            return _uri;
        }
    }
}
