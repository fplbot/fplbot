using Fpl.Search.Models;

namespace Fpl.Search.Indexing;

public class IndexingService(
    IIndexingClient indexingClient,
    IIndexProvider<EntryItem> entryIndexProvider,
    IIndexProvider<LeagueItem> leagueIndexProvider,
    ISingleEntryIndexProvider singleEntryIndexProvider,
    ILogger<IndexingClient> logger)
    : IIndexingService
{
    private readonly ILogger<IndexingClient> _logger = logger;

    public async Task IndexEntries(CancellationToken token, Action<int>? pageProgress = null)
    {
        await Index(entryIndexProvider, pageProgress, token);
    }

    public async Task IndexSingleEntry(int entryId, CancellationToken token)
    {
        var entryItem = await singleEntryIndexProvider.GetSingleEntryToIndex(entryId);
        await indexingClient.Index(new[] {entryItem!}, singleEntryIndexProvider.IndexName, token);
    }

    public async Task IndexLeagues(CancellationToken token, Action<int>? pageProgress = null)
    {
        await Index(leagueIndexProvider, pageProgress, token);
    }

    private async Task Index<T>(IIndexProvider<T> indexProvider, Action<int>? pageProgress, CancellationToken token) where T : class
    {
        var i = await indexProvider.StartIndexingFrom;
        const int batchSize = 8;
        var iteration = 1;
        var shouldContinue = true;

        await indexProvider.Init();

        while (shouldContinue && !token.IsCancellationRequested)
        {
            var (items, couldBeMore) = await indexProvider.GetBatchToIndex(i, batchSize);

            if (items.Any())
            {
                await indexingClient.Index(items, indexProvider.IndexName, token);
            }

            i += batchSize;

            if (couldBeMore && pageProgress != null && iteration % 10 == 0)
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
    Task IndexEntries(CancellationToken token, Action<int>? pageProgress = null);
    Task IndexSingleEntry(int entryId, CancellationToken token);
    Task IndexLeagues(CancellationToken token, Action<int>? pageProgress = null);
}
