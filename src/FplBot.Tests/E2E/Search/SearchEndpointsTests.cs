using System.Net;
using Fpl.Search;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Endpoints.Api.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nest;

namespace FplBot.Tests.E2E;

// Exercises the real SearchService against a real (Testcontainers) Elasticsearch instance —
// data is seeded straight into the index, then the endpoint handler is called and asked to
// find it, instead of faking ISearchService's results.
[Collection("Elasticsearch")]
public class SearchEndpointsTests(ElasticsearchFixture elastic)
{
    private (ISearchService Service, SearchOptions Options, TestPublishEndpoint PublishEndpoint) NewSearchService()
    {
        var options = new SearchOptions
        {
            IndexUri = "unused", Username = "unused", Password = "unused", IndexingCron = "* * * * *",
            EntriesIndex = $"entries-{Guid.NewGuid():N}",
            LeaguesIndex = $"leagues-{Guid.NewGuid():N}",
            AnalyticsIndex = $"analytics-{Guid.NewGuid():N}"
        };
        var publishEndpoint = new TestPublishEndpoint();
        var service = new SearchService(elastic.Client, publishEndpoint, NullLogger<SearchService>.Instance, Options.Create(options));
        return (service, options, publishEndpoint);
    }

    private async Task SeedEntries(string index, params EntryItem[] entries)
    {
        await elastic.Client.IndexManyAsync(entries, index);
        await elastic.Client.Indices.RefreshAsync(index);
    }

    private async Task SeedLeagues(string index, params LeagueItem[] leagues)
    {
        await elastic.Client.IndexManyAsync(leagues, index);
        await elastic.Client.Indices.RefreshAsync(index);
    }

    private static HttpContext HttpContextWithRemoteIp(string? ip = "203.0.113.5")
    {
        var httpContext = new DefaultHttpContext();
        if (ip != null)
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        }

        return httpContext;
    }

    [Fact]
    public async Task GetEntry_Found_ReturnsOkWithEntry()
    {
        var (service, options, _) = NewSearchService();
        var entry = new EntryItem { Id = 42, RealName = "Messi" };
        await elastic.Client.IndexAsync(entry, i => i.Index(options.EntriesIndex).Id(entry.Id), TestContext.Current.CancellationToken);
        await elastic.Client.Indices.RefreshAsync(options.EntriesIndex, ct: TestContext.Current.CancellationToken);

        var result = await SearchEndpoints.GetEntry(42, service);

        var ok = Assert.IsType<Ok<EntryItem>>(result);
        Assert.Equal("Messi", ok.Value!.RealName);
    }

    [Fact]
    public async Task GetEntry_NotFound_ReturnsNotFound()
    {
        var (service, options, _) = NewSearchService();
        await elastic.Client.Indices.CreateAsync(options.EntriesIndex, ct: TestContext.Current.CancellationToken);

        var result = await SearchEndpoints.GetEntry(1, service);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task GetEntries_FindsSeededEntryByRealName()
    {
        var (service, options, _) = NewSearchService();
        await SeedEntries(options.EntriesIndex,
            new EntryItem { Id = 1, RealName = "Lionel Messi" },
            new EntryItem { Id = 2, RealName = "Cristiano Ronaldo" });

        var result = await SearchEndpoints.GetEntries("messi", 0, HttpContextWithRemoteIp(), service);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        SearchResult<EntryItem> hits = value.Hits;
        Assert.Equal(1, hits.TotalHits);
        Assert.Equal("Lionel Messi", hits.ExposedHits.Single().RealName);
    }

    [Fact]
    public async Task GetEntries_PageBeyondResultsAndNoHits_ReturnsBadRequest()
    {
        var (service, options, _) = NewSearchService();
        await elastic.Client.Indices.CreateAsync(options.EntriesIndex, ct: TestContext.Current.CancellationToken);
        await elastic.Client.Indices.RefreshAsync(options.EntriesIndex, ct: TestContext.Current.CancellationToken);

        var result = await SearchEndpoints.GetEntries("nobody", 5, HttpContextWithRemoteIp(), service);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetEntries_RecordsRemoteIpAsActorOnThePublishedAnalyticsEvent()
    {
        var (service, options, publishEndpoint) = NewSearchService();
        await SeedEntries(options.EntriesIndex, new EntryItem { Id = 1, RealName = "Messi" });

        await SearchEndpoints.GetEntries("messi", 0, HttpContextWithRemoteIp("203.0.113.5"), service);

        var indexed = publishEndpoint.PublishedMessages.Containing<FplBot.Messaging.Contracts.Commands.v1.IndexQuery>().Single();
        var query = (FplBot.Messaging.Contracts.Commands.v1.IndexQuery)indexed.Message;
        Assert.Equal("203.0.113.5", query.Actor);
        Assert.Equal(nameof(QueryClient.Web), query.Client);
    }

    [Fact]
    public async Task GetLeagues_FindsSeededLeagueByName()
    {
        var (service, options, _) = NewSearchService();
        await SeedLeagues(options.LeaguesIndex,
            new LeagueItem { Id = 1, Name = "The Gaffers League" },
            new LeagueItem { Id = 2, Name = "Some Other League" });

        var result = await SearchEndpoints.GetLeagues("gaffers", 0, "", HttpContextWithRemoteIp(), service);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        SearchResult<LeagueItem> hits = value.Hits;
        Assert.Equal(1, hits.TotalHits);
        Assert.Equal("The Gaffers League", hits.ExposedHits.Single().Name);
    }

    [Fact]
    public async Task GetLeagues_PageBeyondResultsAndNoHits_ReturnsBadRequest()
    {
        var (service, options, _) = NewSearchService();
        await elastic.Client.Indices.CreateAsync(options.LeaguesIndex, ct: TestContext.Current.CancellationToken);
        await elastic.Client.Indices.RefreshAsync(options.LeaguesIndex, ct: TestContext.Current.CancellationToken);

        var result = await SearchEndpoints.GetLeagues("nobody", 5, "", HttpContextWithRemoteIp(), service);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAny_FindsBothSeededEntriesAndLeagues()
    {
        var (service, options, _) = NewSearchService();
        await SeedEntries(options.EntriesIndex, new EntryItem { Id = 1, RealName = "Skjelbek" });
        await SeedLeagues(options.LeaguesIndex, new LeagueItem { Id = 1, Name = "Skjelbek League" });

        var result = await SearchEndpoints.GetAny("skjelbek", 0, HttpContextWithRemoteIp(), service);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        SearchResult<dynamic> hits = value.Hits;
        Assert.Equal(2, hits.TotalHits);
        Assert.Contains(hits.ExposedHits, h => ((SearchContainer)h).Type == "entry");
        Assert.Contains(hits.ExposedHits, h => ((SearchContainer)h).Type == "league");
    }
}
