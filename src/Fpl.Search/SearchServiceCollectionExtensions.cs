using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using Fpl.Data.Repositories;
using FplBot.Core.RecurringActions;
using Microsoft.Extensions.Options;

namespace Fpl.Search
{
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
            return services;
        }

        public static IServiceCollection AddIndexingServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SearchOptions>(config);
            services.AddSingleton<IIndexingClient, IndexingClient>();

            services.AddSingleton<EntryIndexProvider>();
            services.AddSingleton<IIndexProvider<EntryItem>>(x => x.GetRequiredService<EntryIndexProvider>());
            services.AddSingleton<ISingleEntryIndexProvider>(x => x.GetRequiredService<EntryIndexProvider>());
            services.AddSingleton<IVerifiedEntryIndexProvider>(x => x.GetRequiredService<EntryIndexProvider>());

            services.AddSingleton<IIndexProvider<LeagueItem>, LeagueIndexProvider>();
            services.AddSingleton<IIndexingService, IndexingService>();
            return services;
        }

        public static IServiceCollection AddRecurringIndexer(this IServiceCollection services, IConfiguration config)
        {
            services.AddIndexingServices(config);
            services.AddRecurringActions().AddRecurrer<IndexerRecurringAction>().Build();
            return services;
        }
    }
}
