using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Core.RecurringActions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nest;
using CronBackgroundServices;
using Fpl.Search.Data;
using Fpl.Search.Data.Abstractions;
using Fpl.Search.Data.Repositories;
using FplBot.VerifiedEntries.InternalCommands;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Fpl.Search;

public static class SearchServiceCollectionExtensions
{
    public static IServiceCollection AddSearching(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SearchOptions>(config);
        services.AddSingleton<IElasticClient>(provider =>
        {
            var searchOpts = provider.GetService<IOptions<SearchOptions>>();
            var searchOptions = searchOpts.Value;
            searchOptions.Validate();
            var connectionSettings = new ConnectionSettings(new Uri(searchOptions.IndexUri));
            connectionSettings.BasicAuthentication(searchOptions.Username, searchOptions.Password);
            return new ElasticClient(connectionSettings);
        });

        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IIndexingClient, IndexingClient>();
        return services;
    }

    public static IServiceCollection AddIndexingServices(this IServiceCollection services, IConfiguration config, IConnectionMultiplexer connection)
    {
        services.Configure<SearchRedisOptions>(config);
        services.TryAddSingleton<IConnectionMultiplexer>(connection);
        services.Configure<SearchOptions>(config.GetSection("search"));
        services.AddSingleton<IIndexingClient, IndexingClient>();

        services.AddSingleton<SlowEntryIndexProvider>();
        services.AddSingleton<IIndexProvider<EntryItem>>(x => x.GetRequiredService<SlowEntryIndexProvider>());
        services.AddSingleton<ISingleEntryIndexProvider>(x => x.GetRequiredService<SlowEntryIndexProvider>());
        services.AddSingleton<IVerifiedEntryIndexProvider>(x => x.GetRequiredService<SlowEntryIndexProvider>());

        services.AddSingleton<IIndexProvider<LeagueItem>, LeagueIndexProvider>();
        services.AddSingleton<IIndexingService, IndexingService>();
        services.AddSingleton<ILeagueIndexBookmarkProvider, LeagueIndexRedisBookmarkProvider>();
        services.AddSingleton<IEntryIndexBookmarkProvider, EntryIndexRedisBookmarkProvider>();
        services.AddSingleton<IElasticClient>(provider =>
        {
            var searchOpts = provider.GetService<IOptions<SearchOptions>>();
            var searchOptions = searchOpts.Value;
            searchOptions.Validate();
            var connectionSettings = new ConnectionSettings(new Uri(searchOptions.IndexUri));
            connectionSettings.BasicAuthentication(searchOptions.Username, searchOptions.Password);
            return new ElasticClient(connectionSettings);
        });
        services.AddMediatR(typeof(IndexEntryCommandHandler));

        return services;
    }

    public static IServiceCollection AddRecurringIndexer(this IServiceCollection services, IConfiguration config, IConnectionMultiplexer conn)
    {
        services.AddIndexingServices(config, conn);
        services.AddRecurrer<IndexerRecurringAction>();
        return services;
    }
}
