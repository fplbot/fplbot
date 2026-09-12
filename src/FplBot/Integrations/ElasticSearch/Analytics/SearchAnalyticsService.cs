using Fpl.Search.Models;
using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.Extensions.Options;
using Nest;

namespace Fpl.Search.Analytics;

public record TermCount(string Term, long Count);

public record SearchAnalyticsResult(
    DateTime From,
    DateTime To,
    long TotalQueries,
    IReadOnlyList<TermCount> TopQueries,
    IReadOnlyList<TermCount> TopIpAddresses,
    IReadOnlyList<TermCount> TopSlackSearchers);

public class SearchAnalyticsService(IElasticClient elasticClient, IOptions<SearchOptions> options) : ISearchAnalyticsService
{
    private readonly SearchOptions _options = options.Value;

    public async Task<SearchAnalyticsResult> GetTopSearches(int days, int size)
    {
        var from = DateTime.UtcNow.AddDays(-days);

        var response = await elasticClient.SearchAsync<IndexQuery>(s => s
            .Index(_options.AnalyticsIndex)
            .Size(0)
            .TrackTotalHits(true)
            .Query(q => q.DateRange(d => d.Field("timeStamp").GreaterThanOrEquals(from)))
            .Aggregations(a => a
                .Terms("top_queries", t => t
                    .Field("query.keyword")
                    .Size(size))
                .Filter("web_only", f => f
                    // Actor is an IP address for Web-client searches, a Slack user id for Slack ones.
                    .Filter(ff => ff.Term("client.keyword", nameof(QueryClient.Web)))
                    .Aggregations(aa => aa
                        .Terms("top_ips", t => t
                            .Field("actor.keyword")
                            .Size(size))))
                .Filter("slack_only", f => f
                    .Filter(ff => ff.Term("client.keyword", nameof(QueryClient.Slack)))
                    .Aggregations(aa => aa
                        .Terms("top_slack_searchers", t => t
                            .Field("actor.keyword")
                            .Size(size))))));

        if (!response.IsValid)
        {
            throw new Exception(response.DebugInformation, response.OriginalException);
        }

        var topQueries = ToTermCounts(response.Aggregations.Terms<string>("top_queries"));
        var topIps = ToTermCounts(response.Aggregations.Filter("web_only")?.Terms<string>("top_ips"));
        var topSlackSearchers = ToTermCounts(response.Aggregations.Filter("slack_only")?.Terms<string>("top_slack_searchers"));

        return new SearchAnalyticsResult(from, DateTime.UtcNow, response.Total, topQueries, topIps, topSlackSearchers);
    }

    private static IReadOnlyList<TermCount> ToTermCounts(TermsAggregate<string>? termsAggregate)
    {
        if (termsAggregate == null)
        {
            return [];
        }

        return termsAggregate.Buckets
            .Select(b => new TermCount(b.Key, b.DocCount ?? 0))
            .ToArray();
    }
}

public interface ISearchAnalyticsService
{
    Task<SearchAnalyticsResult> GetTopSearches(int days, int size);
}
