using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search.Data.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Options;

namespace Fpl.Search.Indexing;

public class SlowEntryIndexProvider(
    ILeagueClient leagueClient,
    IEntryClient entryClient,
    IEntryHistoryClient entryHistoryClient,
    IEntryIndexBookmarkProvider indexBookmarkProvider,
    ILogger<IndexProviderBase> logger,
    IOptions<SearchOptions> options)
    : IndexProviderBase(leagueClient, logger), IIndexProvider<EntryItem>, ISingleEntryIndexProvider
{
    private readonly ILogger<IndexProviderBase> _logger = logger;
    private readonly SearchOptions _options = options.Value;
    private int _currentConsecutiveCountOfMissingEntries;
    private int _bookmarkCounter;

    public string IndexName => _options.EntriesIndex;
    public Task<int> StartIndexingFrom => indexBookmarkProvider.GetBookmark();

    public Task Init() => Task.CompletedTask;

    public async Task<(EntryItem[], bool)> GetBatchToIndex(int i, int batchSize)
    {
        var entryBatch = await ClientHelper.PolledRequests(() => Enumerable.Range(i, batchSize).Select(n => entryClient.Get(n, tolerate404: true)).ToArray(), _logger);
        var items = entryBatch
            .Where(x => x != null && x.Exists)
            .Select(y => new EntryItem { Id = y!.Id, TeamName = y.TeamName, RealName = y.PlayerFullName, Country = y.PlayerRegionShortIso }).ToArray();

        if (!items.Any())
        {
            _currentConsecutiveCountOfMissingEntries += batchSize;
        }
        else
        {
            var historyBatch = (await ClientHelper.PolledRequests(() => Enumerable.Range(i, batchSize).Select(n => entryHistoryClient.GetHistory(n, tolerate404: true)).ToArray(), _logger))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToArray();

            foreach (var entryItem in items)
            {
                var (_, entryHistory) = historyBatch.Single(x => x.teamId == entryItem.Id);
                entryItem.NumberOfPastSeasons = entryHistory.SeasonHistory.Count;
                entryItem.Thumbprint = ToEntryThumbprint(entryHistory);
            }

            _currentConsecutiveCountOfMissingEntries = 0;
        }

        // There are large "gaps" of missing entries (deleted ones, perhaps). The indexing job needs to work its way past these gaps, but still stop when
        // we think that there are none left to index
        var couldBeMore = _currentConsecutiveCountOfMissingEntries <
                          _options.ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob;

        if (!couldBeMore)
        {
            if (_options.ResetIndexingBookmarkWhenDone)
            {
                await indexBookmarkProvider.SetBookmark(1);
            }
            else
            {
                var resetBookmarkTo = i - _options.ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob;
                await indexBookmarkProvider.SetBookmark(resetBookmarkTo > 1 ? resetBookmarkTo : 1);
            }
        }
        else if (_bookmarkCounter > 50) // Set a bookmark at every 50th batch
        {
            await indexBookmarkProvider.SetBookmark(i + batchSize);
            _bookmarkCounter = 0;
        }
        else
        {
            _bookmarkCounter++;
        }

        return (items, couldBeMore);
    }

    public async Task<EntryItem?> GetSingleEntryToIndex(int entryId)
    {
        var entry = await entryClient.Get(entryId);
        var history = (await entryHistoryClient.GetHistory(entryId))?.entryHistory;

        return new EntryItem
        {
            Id = entry!.Id,
            RealName = entry.PlayerFullName,
            TeamName = entry.TeamName,
            Country = entry.PlayerRegionShortIso,
            NumberOfPastSeasons = history != null ? history.SeasonHistory.Count : 0,
            Thumbprint = history != null ? ToEntryThumbprint(history) : string.Empty
        };
    }

    private static string ToEntryThumbprint(EntryHistory entryHistory)
    {
        return entryHistory.SeasonHistory.Any() ? string.Join(";", entryHistory.SeasonHistory.Take(2).Select(x => $"{x.SeasonName}:{x.Rank}").ToArray()) : string.Empty;
    }
}
