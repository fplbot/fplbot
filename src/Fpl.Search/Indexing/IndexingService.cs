using System.Linq;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class IndexingService : IIndexingService
    {
        private readonly ILogger<IndexingClient> _logger;
        private readonly IIndexingClient _indexingClient;
        private readonly IIndexProvider<EntryItem> _entryIndexProvider;
        private readonly IIndexProvider<LeagueItem> _leagueIndexProvider;
        private bool _isCancelled;

        public IndexingService(
            IIndexingClient indexingClient,
            IIndexProvider<EntryItem> entryIndexProvider,
            IIndexProvider<LeagueItem> leagueIndexProvider,
            ILogger<IndexingClient> logger)
        {
            _indexingClient = indexingClient;
            _entryIndexProvider = entryIndexProvider;
            _leagueIndexProvider = leagueIndexProvider;
            _logger = logger;
        }

        public async Task IndexEntries()
        {
            await Index(_entryIndexProvider);
        }

        public async Task IndexLeagues()
        {
            await Index(_leagueIndexProvider);
        }

        public void Cancel()
        {
            _isCancelled = true;
        }

        private async Task Index<T>(IIndexProvider<T> indexProvider) where T : class, IIndexableItem
        {
            var i = await indexProvider.StartIndexingFrom;
            var batchSize = 8;
            var iteration = 1;
            var shouldContinue = true;

            while (shouldContinue && !_isCancelled)
            {
                var (items, couldBeMore) = await indexProvider.GetBatchToIndex(i, batchSize);

                if (items.Any())
                {
                    await _indexingClient.Index(items, indexProvider.IndexName);
                }

                i += batchSize;

                if (iteration % 10 == 0)
                {
                    _logger.LogInformation("Indexed page {i}", i);
                }

                shouldContinue = couldBeMore;
                iteration++;
            }
        }
    }

    public interface IIndexingService
    {
        Task IndexEntries();
        Task IndexLeagues();
        void Cancel();
    }
}
