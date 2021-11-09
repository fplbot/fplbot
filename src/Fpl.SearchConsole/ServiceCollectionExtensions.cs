using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Search;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using Fpl.Search.Data.Abstractions;
using StackExchange.Redis;

namespace Fpl.SearchConsole;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchConsole(this IServiceCollection services, IConfiguration configuration, ConnectionMultiplexer connection)
    {
        services.AddSearching(configuration.GetSection("Search"));
        services.AddIndexingServices(configuration, connection);

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