using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;

namespace Fpl.Search
{
    public static class SearchServiceCollectionExtensions
    {
        public static IServiceCollection AddSearch(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SearchOptions>(config);

            services.AddSingleton<IElasticClient>(provider =>
            {
                var searchOptions = config.Get<SearchOptions>();
                searchOptions.Validate();
                var connectionSettings = new ConnectionSettings(new Uri(searchOptions.IndexUri));
                connectionSettings.BasicAuthentication(searchOptions.Username, searchOptions.Password);
                return new ElasticClient(connectionSettings);
            });

            services.AddSingleton<IIndexingClient, IndexingClient>();
            services.AddSingleton<ISearchService, SearchService>();

            services.AddSingleton<IIndexProvider<EntryItem>, EntryIndexProvider>();
            services.AddSingleton<IIndexProvider<LeagueItem>, LeagueIndexProvider>();
            services.AddSingleton<IIndexBookmarkProvider, LeagueIndexBookmarkProvider>();
            services.AddSingleton<IIndexingService, IndexingService>();

            return services;
        }
    }
}