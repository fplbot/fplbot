using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class IndexingClient : ElasticClientBase, IIndexingClient
    {
        private readonly ILogger<IndexingClient> _logger;

        public IndexingClient(ILogger<IndexingClient> logger, SearchOptions options) : base(options)
        {
            _logger = logger;
        }

        public async Task Index(Entry[] entries, string index)
        {
            var response = await Client.IndexManyAsync(entries, index);
            if (response.Errors)
            {
                foreach (var itemWithError in response.ItemsWithErrors)
                {
                    _logger.LogError($"Failed to index document {itemWithError.Id}: {itemWithError.Error}");
                }
            }
        }
    }

    public interface IIndexingClient : IElasticClientBase
    {
        Task Index(Entry[] entries, string index);
    }
}
