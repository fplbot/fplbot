using Fpl.Search.Models;
using Microsoft.Extensions.Logging;

namespace Fpl.Search.Indexing;

public class IndexingService : IIndexingService
{
    private readonly ILogger<IndexingClient> _logger;
    private readonly IIndexingClient _indexingClient;
    private readonly IIndexProvider<EntryItem> _entryIndexProvider;
    private readonly IIndexProvider<LeagueItem> _leagueIndexProvider;
    private readonly IVerifiedEntryIndexProvider _verifiedEntryIndexProvider;
    private readonly ISingleEntryIndexProvider _singleEntryIndexProvider;

    public IndexingService(
        IIndexingClient indexingClient,
        IIndexProvider<EntryItem> entryIndexProvider,
        IIndexProvider<LeagueItem> leagueIndexProvider,
        IVerifiedEntryIndexProvider verifiedEntryIndexProvider,
        ISingleEntryIndexProvider singleEntryIndexProvider,
        ILogger<IndexingClient> logger)
    {
        _indexingClient = indexingClient;
        _entryIndexProvider = entryIndexProvider;
        _leagueIndexProvider = leagueIndexProvider;
        _verifiedEntryIndexProvider = verifiedEntryIndexProvider;
        _singleEntryIndexProvider = singleEntryIndexProvider;
        _logger = logger;
    }

    public async Task IndexEntries(CancellationToken token, Action<int> pageProgress = null)
    {
        await IndexVerifiedEntries(token);
        await Index(_entryIndexProvider, pageProgress, token);
    }

    public async Task IndexSingleEntry(int entryId, CancellationToken token)
    {
        var entryItem = await _singleEntryIndexProvider.GetSingleEntryToIndex(entryId);
        await _indexingClient.Index(new[] {entryItem}, _singleEntryIndexProvider.IndexName, token);
    }

    private async Task IndexVerifiedEntries(CancellationToken token)
    {
        var verifiedEntriesToIndex = await _verifiedEntryIndexProvider.GetAllVerifiedEntriesToIndex();
        if (verifiedEntriesToIndex.Any())
        {
            await _indexingClient.Index(verifiedEntriesToIndex, _verifiedEntryIndexProvider.IndexName, token);
        }
    }

    public async Task IndexLeagues(CancellationToken token, Action<int> pageProgress = null)
    {
        await Index(_leagueIndexProvider, pageProgress, token);
    }

    private async Task Index<T>(IIndexProvider<T> indexProvider, Action<int> pageProgress, CancellationToken token) where T : class
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
                await _indexingClient.Index(items, indexProvider.IndexName, token);
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
    Task IndexEntries(CancellationToken token, Action<int> pageProgress = null);
    Task IndexSingleEntry(int entryId, CancellationToken token);
    Task IndexLeagues(CancellationToken token, Action<int> pageProgress = null);
}
