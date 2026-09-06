using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Clients;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class IFplApiClientServiceCollectionExtensions
{
    public static IServiceCollection AddFplApiClient(this IServiceCollection services, IConfiguration config)
    {
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
        return services;
    }
}
