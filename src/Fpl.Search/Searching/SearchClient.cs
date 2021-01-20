using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

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

        public async Task<IReadOnlyCollection<EntryItem>> SearchForEntry(string query, int maxHits)
        {
            return await Search<EntryItem>(query, maxHits, _options.EntriesIndex, f => f.RealName, f => f.TeamName);
        }

        public async Task<IReadOnlyCollection<LeagueItem>> SearchForLeague(string query, int maxHits)
        {
            return await Search<LeagueItem>(query, maxHits, _options.LeaguesIndex, f => f.Name);
        }

        private async Task<IReadOnlyCollection<T>> Search<T>(string query, int maxHits, string index, params Expression<Func<T, object>>[] fields) where T : class
        {
            var response = await _elasticClient.SearchAsync<T>(x => x
                .Index(index)
                .From(0)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Fields(fields))
                        .Query(query))));

            _logger.LogInformation("Search for \"{query}\" got {totalHits} in the {index} index. Returned {returned} of them.", query, response.Total, response.Documents.Count, index);

            return response.Documents;
        }
    }

    public interface ISearchClient
    {
        Task<IReadOnlyCollection<EntryItem>> SearchForEntry(string query, int maxHits);
        Task<IReadOnlyCollection<LeagueItem>> SearchForLeague(string query, int maxHits);
    }
}
