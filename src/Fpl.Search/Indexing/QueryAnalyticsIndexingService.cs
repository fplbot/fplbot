using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class QueryAnalyticsIndexingService : IQueryAnalyticsIndexingService
    {
        private readonly IIndexingClient _indexingClient;
        private readonly SearchOptions _options;
        private readonly ILogger<QueryAnalyticsIndexingService> _logger;

        public QueryAnalyticsIndexingService(
            IIndexingClient indexingClient,
            IOptions<SearchOptions> options,
            ILogger<QueryAnalyticsIndexingService> logger)
        {
            _indexingClient = indexingClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task IndexQuery(string query, string queriedIndex, string boostedCountry, long totalHits, long responseTimeMs, QueryClient? client, string team, string followingFplLeagueId, string actor)
        {
            try
            {
                await _indexingClient.Index(new[] { new QueryAnalyticsItem
                {
                    TimeStamp = DateTime.UtcNow,
                    Query = query,
                    QueriedIndex = queriedIndex,
                    BoostedCountry = boostedCountry,
                    TotalHits = totalHits,
                    ResponseTimeMs = responseTimeMs,
                    Client = client.ToString(),
                    Team = team,
                    FollowingFplLeagueId = followingFplLeagueId,
                    Actor = actor
                }}, _options.AnalyticsIndex, new CancellationToken());

                _logger.LogInformation("Indexed query \"{query}\"", query);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to index query \"{query}\"", query);
            }
        }
    }

    public interface IQueryAnalyticsIndexingService
    {
        Task IndexQuery(string query, string queriedIndex, string boostedCountry, long totalHits, long responseTimeMs, QueryClient? client, string team, string followingFplLeagueId, string actor);
    }
}