using System;
using System.Net;
using System.Net.Http;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nest;

namespace Fpl.SearchConsole
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSearchConsole(this IServiceCollection services)
        {
            var options = new Options();
            var conn = new ConnectionSettings(new Uri(options.Value.IndexUri));
            if (!string.IsNullOrEmpty(options.Value.Username) && !string.IsNullOrEmpty(options.Value.Password))
            {
                conn.BasicAuthentication(options.Value.Username, options.Value.Password);
            }
            services.AddSingleton<IOptions<SearchOptions>>(options);
            services.AddSingleton<IConnectionSettingsValues>(conn);
            services.AddSingleton<IElasticClient, ElasticClient>();
            services.AddSingleton<IIndexingClient, IndexingClient>();
            services.AddHttpClient<ILeagueClient, LeagueClient>(_ => new LeagueClient(CreateHttpClient()));
            services.AddHttpClient<IEntryClient, EntryClient>(_ => new EntryClient(CreateHttpClient()));
            services.AddSingleton<IIndexBookmarkProvider, SimpleLeagueIndexBookmarkProvider>();
            services.AddSingleton<IIndexProvider<EntryItem>, EntryIndexProvider>();
            services.AddSingleton<IIndexProvider<LeagueItem>, LeagueIndexProvider>();
            services.AddSingleton<IIndexingService, IndexingService>();
            services.AddSingleton<ISearchService, SearchService>();
            return services;
        }

        private static HttpClient CreateHttpClient()
        {
            var httpMessageHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };
            var c = new HttpClient(httpMessageHandler)
            {
                BaseAddress = new Uri($"https://fantasy.premierleague.com")
            };
            c.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            c.DefaultRequestHeaders.Add("User-Agent", "Lol");
            return c;
        }
    }
}