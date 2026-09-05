using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Clients;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

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
        services.AddHttpClient<IEntryClient, EntryClient>();
        services.AddHttpClient<IEntryHistoryClient, EntryHistoryClient>();
        services.AddHttpClient<IFixtureClient, FixtureClient>();
        services.AddHttpClient<ILeagueClient, LeagueClient>();
        services.AddHttpClient<ITransfersClient, TransfersClient>();
        services.AddHttpClient<IGlobalSettingsClient, GlobalSettingsClient>();
        services.AddHttpClient<ILiveClient, LiveClient>();
        services.AddHttpClient<IEventStatusClient, EventStatusClient>();
        services.ConfigureOptions<FplClientOptionsConfigurator>();
        services.AddSingleton<Authenticator>();
        services.AddSingleton<CookieFetcher>();
        services.AddSingleton<CookieCache>();
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
