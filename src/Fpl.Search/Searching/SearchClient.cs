using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Search.Searching
{
    public class SearchClient : ISearchClient
    {
        private readonly IElasticClient _client;
        private readonly ILogger<SearchClient> _logger;

        public SearchClient(ILogger<SearchClient> logger, SearchOptions options)
        {
            _client = new ElasticClient(new Uri(options.IndexUri));
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<Entry>> Search(string query, int maxHits)
        {
            var response = await _client.SearchAsync<Entry>(x => x
                .From(0)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Field(y => y.RealName).Field(y => y.TeamName))
                        .Query(query))));

            return response.Documents;
        }
    }

    public interface ISearchClient
    {
        Task<IReadOnlyCollection<Entry>> Search(string query, int maxHits);
    }
}
