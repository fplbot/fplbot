using Nest;

namespace Fpl.Search.Indexing;

public class IndexingClient(IElasticClient elasticClient, ILogger<IndexingClient> logger)
    : IIndexingClient
{
    public async Task Index<T>(IEnumerable<T> items, string index, CancellationToken token) where T : class
    {
        var response = await elasticClient.IndexManyAsync(items, index, token);
        if (response.Errors)
        {
            foreach (var itemWithError in response.ItemsWithErrors)
            {
                logger.LogError($"Failed to index document {itemWithError.Id}: {itemWithError.Error}");
            }
        }
    }
}

public interface IIndexingClient
{
    Task Index<T>(IEnumerable<T> items, string index, CancellationToken token) where T : class;
}
