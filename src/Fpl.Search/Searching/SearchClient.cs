using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Search.Searching
{
    public class SearchClient : ElasticClientBase, ISearchClient
    {
        private readonly ILogger<SearchClient> _logger;

        public SearchClient(ILogger<SearchClient> logger, SearchOptions options) : base(options)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<Entry>> Search(string query, int maxHits, string index)
        {
            var response = await Client.SearchAsync<Entry>(x => x
                .Index(index)
                .From(0)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Field(y => y.RealName).Field(y => y.TeamName))
                        .Query(query))));

            _logger.LogInformation("A search for {query} got {hits} hits. Returned {returned} of them due to {maxHits} maxHits.", 
                query, response.Total, response.Documents.Count, maxHits);

            return response.Documents;
        }
    }

    public interface ISearchClient : IElasticClientBase
    {
        Task<IReadOnlyCollection<Entry>> Search(string query, int maxHits, string index);
    }
}
