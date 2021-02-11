using System;
using System.Linq;
using System.Threading;
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

        public async Task IndexEntries(CancellationToken token, Action<int> pageProgress = null)
        {
            await Index(_entryIndexProvider, pageProgress,token);
        }

        public async Task IndexLeagues(CancellationToken token, Action<int> pageProgress = null)
        {
            await Index(_leagueIndexProvider,pageProgress,token);
        }

        private async Task Index<T>(IIndexProvider<T> indexProvider, Action<int> pageProgress, CancellationToken token) where T : class
        {
            var i = await indexProvider.StartIndexingFrom;
            var batchSize = 8;
            var iteration = 1;
            var shouldContinue = true;

            while (shouldContinue && !token.IsCancellationRequested)
            {
                var (items, couldBeMore) = await indexProvider.GetBatchToIndex(i, batchSize);

                if (items.Any())
                {
                    await _indexingClient.Index(items, indexProvider.IndexName, token);
                }

                i += batchSize;

                if (pageProgress != null && iteration % 10 == 0)
                {
                    pageProgress(i);
                }
                shouldContinue = couldBeMore;
                iteration++;
            }
        }
    }

    public interface IIndexingService
    {
        Task IndexEntries(CancellationToken token, Action<int> pageProgress = null);
        Task IndexLeagues(CancellationToken token, Action<int> pageProgress = null);
    }
}
