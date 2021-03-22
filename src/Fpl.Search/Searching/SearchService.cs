using System;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace Fpl.Search.Searching
{
    public class SearchService : ISearchService
    {
        private readonly IElasticClient _elasticClient;
        private readonly IMessageSession _messageSession;
        private readonly ILogger<SearchService> _logger;
        private readonly SearchOptions _options;

        public SearchService(
            IElasticClient elasticClient,
            IMessageSession messageSession,
            ILogger<SearchService> logger,
            IOptions<SearchOptions> options)
        {
            _elasticClient = elasticClient;
            _messageSession = messageSession;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<SearchResult<EntryItem>> SearchForEntry(string query, int page, int maxHits, SearchMetaData metaData)
        {
            var response = await _elasticClient.SearchAsync<EntryItem>(x => x
                .Index(_options.EntriesIndex)
                .From(page * maxHits)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Field(y => y.RealName, 1.5).Field(y => y.TeamName).Field(y => y.Alias).Field(y => y.Description, 0.5))
                        .Query(query)
                        .Fuzziness(Fuzziness.Auto))
                    )
                .Sort(sd => sd
                    .Descending(SortSpecialField.Score)
                    .Descending(p => p.VerifiedType != null ? 1 : 0)
                    .Ascending(SortSpecialField.DocumentIndexOrder))
                .Preference(metaData?.Actor)
                );

            _logger.LogInformation("Entry search for {query} returned {returned} of {hits} hits.", query, response.Hits.Count, response.Total);

            await _messageSession.SendLocal(new IndexQuery(DateTime.UtcNow, query, page, _options.EntriesIndex, null, response.Total, response.Took, metaData?.Client.ToString(), metaData?.Team, metaData?.FollowingFplLeagueId, metaData?.Actor));

            return new SearchResult<EntryItem>(response.Hits.Select(h => h.Source).ToArray(), response.Total, page, maxHits);
        }

        public async Task<EntryItem> GetEntry(int id)
        {
            var response = await _elasticClient.GetAsync<EntryItem>(id, desc => desc.Index(_options.EntriesIndex));

            return response.Found ? response.Source : null;
        }

        public async Task<SearchResult<LeagueItem>> SearchForLeague(string query, int page, int maxHits, SearchMetaData metaData, string countryToBoost = null)
        {
            var response = await _elasticClient.SearchAsync<LeagueItem>(x => x
                .Index(_options.LeaguesIndex)
                .From(page * maxHits)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Field(y => y.Name, 1.5).Field(y => y.AdminName))
                        .Query(query)
                        .Fuzziness(Fuzziness.Auto))
                    )
                .Sort(sd => sd
                    .Descending(SortSpecialField.Score)
                    .Ascending(SortSpecialField.DocumentIndexOrder))
                .Preference(metaData?.Actor)
                );

            _logger.LogInformation("League search for {query} returned {returned} of {hits} hits.", query, response.Hits.Count, response.Total);

            await _messageSession.SendLocal(new IndexQuery(DateTime.UtcNow, query, page, _options.LeaguesIndex,countryToBoost,response.Total, response.Took, metaData?.Client.ToString(), metaData?.Team, metaData?.FollowingFplLeagueId, metaData?.Actor));

            return new SearchResult<LeagueItem>(response.Hits.Select(h => h.Source).ToArray(), response.Total, page, maxHits);
        }
    }

    public interface ISearchService
    {
        Task<SearchResult<EntryItem>> SearchForEntry(string query, int page, int maxHits, SearchMetaData metaData);
        Task<EntryItem> GetEntry(int id);
        Task<SearchResult<LeagueItem>> SearchForLeague(string query, int page, int maxHits, SearchMetaData metaData, string countryToBoost = null);
    }
}
