using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class IndexingClient : IIndexingClient
    {
        private readonly IElasticClient _client;
        private readonly ILogger<IndexingClient> _logger;

        public IndexingClient(ILogger<IndexingClient> logger, SearchOptions options)
        {
            _client = new ElasticClient(new Uri(options.IndexUri));
            _logger = logger;
        }

        public async Task Index(Entry[] entries)
        {
            var response = await _client.IndexManyAsync(entries);
            if (response.Errors)
            {
                foreach (var itemWithError in response.ItemsWithErrors)
                {
                    _logger.LogError($"Failed to index document {itemWithError.Id}: {itemWithError.Error}");
                }
            }
        }
    }

    public interface IIndexingClient
    {
        Task Index(Entry[] entries);
    }
}
