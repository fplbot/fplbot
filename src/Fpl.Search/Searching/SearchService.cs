using System;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<SearchResult<EntryItem>> SearchForEntry(string query, int maxHits, SearchMetaData metaData)
        {
         

            var response = await _elasticClient.SearchAsync<EntryItem>(x => x
                .Index(_options.EntriesIndex)
                .From(0)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Field(y => y.RealName, 1.5).Field(y => y.TeamName))
                        .Query(query))));

            var hits = response.Hits.OrderByDescending(h => h.Score)
                .ThenByDescending(h => h.Source.VerifiedType != null ? 1 : 0);
            var entryItems = hits.Select(h => h.Source).ToArray();

            _logger.LogInformation("Entry search for {query} returned {returned} of {hits} hits.", query, entryItems.Length, response.Total);

            await _messageSession.SendLocal(new IndexQuery(DateTime.UtcNow, query, _options.EntriesIndex,null,response.Total, response.Took, metaData?.Client.ToString(), metaData?.Team, metaData?.FollowingFplLeagueId, metaData?.Actor));

            return new SearchResult<EntryItem>(entryItems, response.Total);
        }

        public async Task<SearchResult<LeagueItem>> SearchForLeague(string query, int maxHits, SearchMetaData metaData, string countryToBoost = null)
        {
            var response = await _elasticClient.SearchAsync<LeagueItem>(x => x
                .Index(_options.LeaguesIndex)
                .From(0)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Fields(y => y.Name))
                        .Query(query))));

            var leagueItems = response.Documents;

            if (!string.IsNullOrEmpty(countryToBoost))
            {
                var hits = response.Hits.OrderByDescending(h => h.Score)
                    .ThenByDescending(h => h.Source.AdminCountry == countryToBoost ? 1 : 0);
                leagueItems = hits.Select(h => h.Source).ToArray();
            }

            _logger.LogInformation("League search for {query} returned {returned} of {hits} hits.", query, leagueItems.Count, response.Total);

            await _messageSession.SendLocal(new IndexQuery(DateTime.UtcNow, query, _options.LeaguesIndex,countryToBoost,response.Total, response.Took, metaData?.Client.ToString(), metaData?.Team, metaData?.FollowingFplLeagueId, metaData?.Actor));

            return new SearchResult<LeagueItem>(leagueItems, response.Total);
        }
    }

    public interface ISearchService
    {
        Task<SearchResult<EntryItem>> SearchForEntry(string query, int maxHits, SearchMetaData metaData);
        Task<SearchResult<LeagueItem>> SearchForLeague(string query, int maxHits, SearchMetaData metaData, string countryToBoost = null);
    }
}