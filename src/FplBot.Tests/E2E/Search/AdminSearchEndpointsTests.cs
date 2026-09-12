using Fpl.Search;
using Fpl.Search.Analytics;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Nest;

namespace FplBot.Tests.E2E;

// Exercises the real SearchAnalyticsService against a real (Testcontainers) Elasticsearch instance —
// query events are seeded straight into the analytics index, then the admin endpoint is called
// and asked to aggregate them, instead of faking ISearchAnalyticsService's results.
[Collection("Elasticsearch")]
public class AdminSearchEndpointsTests(ElasticsearchFixture elastic)
{
    private static IndexQuery Query(string query, string? actor, string client, DateTime? timeStamp = null) =>
        new(timeStamp ?? DateTime.UtcNow, query, 0, "entries-index", null, 1, 5, client, null, null, actor);

    private (ISearchAnalyticsService Service, string AnalyticsIndex) NewAnalyticsService()
    {
        var analyticsIndex = $"analytics-{Guid.NewGuid():N}";
        var options = Options.Create(new SearchOptions
        {
            IndexUri = "unused", Username = "unused", Password = "unused", IndexingCron = "* * * * *",
            EntriesIndex = "unused", LeaguesIndex = "unused", AnalyticsIndex = analyticsIndex
        });
        return (new SearchAnalyticsService(elastic.Client, options), analyticsIndex);
    }

    [Fact]
    public async Task GetSearchAnalytics_AggregatesTopQueriesAcrossAllClients()
    {
        var (service, index) = NewAnalyticsService();
        await elastic.Client.IndexManyAsync([
            Query("messi", "203.0.113.5", "Web"),
            Query("messi", "203.0.113.9", "Web"),
            Query("messi", "U999", "Slack"),
            Query("ronaldo", "203.0.113.9", "Web"),
        ], index);
        await elastic.Client.Indices.RefreshAsync(index);

        var result = await AdminSearchEndpoints.GetSearchAnalytics(7, 20, service);

        var ok = Assert.IsType<Ok<SearchAnalyticsResult>>(result);
        Assert.Equal(4, ok.Value!.TotalQueries);
        Assert.Equal(new TermCount("messi", 3), ok.Value.TopQueries[0]);
        Assert.Equal(new TermCount("ronaldo", 1), ok.Value.TopQueries[1]);
    }

    [Fact]
    public async Task GetSearchAnalytics_TopIpAddresses_OnlyCountsWebClientSearches()
    {
        var (service, index) = NewAnalyticsService();
        await elastic.Client.IndexManyAsync([
            Query("messi", "203.0.113.5", "Web"),
            Query("ronaldo", "203.0.113.5", "Web"),
            Query("messi", "U999", "Slack"), // actor is a Slack user id, not an IP — must be excluded
        ], index);
        await elastic.Client.Indices.RefreshAsync(index);

        var result = await AdminSearchEndpoints.GetSearchAnalytics(7, 20, service);

        var ok = Assert.IsType<Ok<SearchAnalyticsResult>>(result);
        var ipCounts = ok.Value!.TopIpAddresses;
        Assert.Equal(new TermCount("203.0.113.5", 2), Assert.Single(ipCounts));
    }

    [Fact]
    public async Task GetSearchAnalytics_TopSlackSearchers_OnlyCountsSlackClientSearches_AndIsNotBlendedWithIps()
    {
        var (service, index) = NewAnalyticsService();
        await elastic.Client.IndexManyAsync([
            Query("messi", "U111", "Slack"),
            Query("ronaldo", "U111", "Slack"),
            Query("messi", "U222", "Slack"),
            Query("messi", "203.0.113.5", "Web"), // actor is an IP, not a Slack user id — must be excluded
        ], index);
        await elastic.Client.Indices.RefreshAsync(index);

        var result = await AdminSearchEndpoints.GetSearchAnalytics(7, 20, service);

        var ok = Assert.IsType<Ok<SearchAnalyticsResult>>(result);
        var slackCounts = ok.Value!.TopSlackSearchers;
        Assert.Equal(new TermCount("U111", 2), slackCounts[0]);
        Assert.Equal(new TermCount("U222", 1), slackCounts[1]);
        Assert.DoesNotContain(slackCounts, s => s.Term == "203.0.113.5");
        Assert.DoesNotContain(ok.Value.TopIpAddresses, s => s.Term is "U111" or "U222");
    }

    [Fact]
    public async Task GetSearchAnalytics_ExcludesQueriesOutsideTheDayWindow()
    {
        var (service, index) = NewAnalyticsService();
        await elastic.Client.IndexManyAsync([
            Query("recent", "203.0.113.5", "Web", DateTime.UtcNow.AddDays(-1)),
            Query("ancient", "203.0.113.5", "Web", DateTime.UtcNow.AddDays(-100)),
        ], index);
        await elastic.Client.Indices.RefreshAsync(index);

        var result = await AdminSearchEndpoints.GetSearchAnalytics(7, 20, service);

        var ok = Assert.IsType<Ok<SearchAnalyticsResult>>(result);
        Assert.Equal(1, ok.Value!.TotalQueries);
        Assert.Equal("recent", Assert.Single(ok.Value.TopQueries).Term);
    }
}
