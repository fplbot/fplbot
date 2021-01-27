using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Search.Searching
{
    public class SearchClient : ISearchClient
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<SearchClient> _logger;
        private readonly SearchOptions _options;

        public SearchClient(IElasticClient elasticClient, ILogger<SearchClient> logger, IOptions<SearchOptions> options)
        {
            _elasticClient = elasticClient;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<SearchResult<EntryItem>> SearchForEntry(string query, int maxHits)
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

            return new SearchResult<EntryItem>(entryItems, response.Total);
        }

        public async Task<SearchResult<LeagueItem>> SearchForLeague(string query, int maxHits, string countryToBoost = null)
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

            return new SearchResult<LeagueItem>(leagueItems, response.Total);
        }
    }

    public interface ISearchClient
    {
        Task<SearchResult<EntryItem>> SearchForEntry(string query, int maxHits);
        Task<SearchResult<LeagueItem>> SearchForLeague(string query, int maxHits, string countryToBoost = null);
    }
}
