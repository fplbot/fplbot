using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Config;
using Microsoft.Extensions.Options;
using Nest;
using Fpl.Search.Data.Abstractions;
using Fpl.Search.Data.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Fpl.Search;

public static class SearchServiceCollectionExtensions
{
    public static IServiceCollection AddSearching(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<SearchOptions>()
            .Bind(config)
            .ValidateWithFluentValidation(new SearchOptionsValidator())
            .ValidateOnStart();
        services.AddSingleton<IElasticClient>(provider =>
        {
            var searchOptions = provider.GetRequiredService<IOptions<SearchOptions>>().Value;
            var connectionSettings = new ConnectionSettings(new Uri(searchOptions.IndexUri));
            connectionSettings.BasicAuthentication(searchOptions.Username, searchOptions.Password);
            return new ElasticClient(connectionSettings);
        });

        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IIndexingClient, IndexingClient>();
        return services;
    }

    public static IServiceCollection AddIndexingServices(this IServiceCollection services, IConfiguration config, IConnectionMultiplexer connection)
    {
        services.TryAddSingleton<IConnectionMultiplexer>(connection);
        services.AddOptions<SearchOptions>()
            .Bind(config.GetSection("search"))
            .ValidateWithFluentValidation(new SearchOptionsValidator())
            .ValidateOnStart();
        services.AddSingleton<IIndexingClient, IndexingClient>();

        services.AddSingleton<SlowEntryIndexProvider>();
        services.AddSingleton<IIndexProvider<EntryItem>>(x => x.GetRequiredService<SlowEntryIndexProvider>());
        services.AddSingleton<ISingleEntryIndexProvider>(x => x.GetRequiredService<SlowEntryIndexProvider>());

        services.AddSingleton<IIndexProvider<LeagueItem>, LeagueIndexProvider>();
        services.AddSingleton<IIndexingService, IndexingService>();
        services.AddSingleton<ILeagueIndexBookmarkProvider, LeagueIndexRedisBookmarkProvider>();
        services.AddSingleton<IEntryIndexBookmarkProvider, EntryIndexRedisBookmarkProvider>();
        services.AddSingleton<IElasticClient>(provider =>
        {
            var searchOptions = provider.GetRequiredService<IOptions<SearchOptions>>().Value;
            var connectionSettings = new ConnectionSettings(new Uri(searchOptions.IndexUri));
            connectionSettings.BasicAuthentication(searchOptions.Username, searchOptions.Password);
            return new ElasticClient(connectionSettings);
        });
        return services;
    }

}
