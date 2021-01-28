using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class IndexingClient : IIndexingClient
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<IndexingClient> _logger;

        public IndexingClient(IElasticClient elasticClient, ILogger<IndexingClient> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task Index<T>(IEnumerable<T> items, string index, CancellationToken token) where T : class, IIndexableItem
        {
            var response = await _elasticClient.IndexManyAsync(items, index, token);
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
        Task Index<T>(IEnumerable<T> items, string index, CancellationToken token) where T : class, IIndexableItem;
    }
}
