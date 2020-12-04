using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System.Collections.Generic;
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

        public async Task IndexEntries(EntryItem[] entries)
        {
            await Index(entries, Constants.EntriesIndex);
        }

        public async Task IndexLeagues(LeagueItem[] leagues)
        {
            await Index(leagues, Constants.LeaguesIndex);
        }

        private async Task Index<T>(IEnumerable<T> items, string index) where T : class
        {
            var response = await Client.IndexManyAsync(items, index);
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
        Task IndexEntries(EntryItem[] entries);
        Task IndexLeagues(LeagueItem[] leagues);
    }
}
