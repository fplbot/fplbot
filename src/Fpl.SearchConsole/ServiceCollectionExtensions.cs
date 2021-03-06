using System;
using System.Net;
using System.Net.Http;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Data;
using Fpl.Data.Repositories;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nest;

namespace Fpl.SearchConsole
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSearchConsole(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSearching(configuration.GetSection("Search"));
            services.AddData(configuration);
            services.AddIndexingServices(configuration.GetSection("Search"));

            services.RemoveAll<IIndexBookmarkProvider>();
            services.AddSingleton<IIndexBookmarkProvider, SimpleLeagueIndexBookmarkProvider>();

            services.AddHttpClient<ILeagueClient, LeagueClient>(_ => new LeagueClient(CreateHttpClient()));
            services.AddHttpClient<IEntryClient, EntryClient>(_ => new EntryClient(CreateHttpClient()));
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
