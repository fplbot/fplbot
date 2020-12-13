using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;

namespace Fpl.Search
{
    public static class SearchServiceCollectionExtensions
    {
        public static IServiceCollection AddSearch<T>(this IServiceCollection services, string indexUri)
        {
            services.AddSingleton<IElasticClient>(new ElasticClient(new ConnectionSettings(new Uri(indexUri))));

            services.AddSingleton<IIndexingClient, IndexingClient>();
            services.AddSingleton<ISearchClient, SearchClient>();

            services.AddSingleton<IIndexProvider<EntryItem>, EntryIndexProvider>();
            services.AddSingleton<IIndexProvider<LeagueItem>, LeagueIndexProvider>();
            services.AddSingleton<IIndexingService, IndexingService>();

            return services;
        }
    }
}