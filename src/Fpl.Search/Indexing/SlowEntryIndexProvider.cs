using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search.Data.Abstractions;
using Fpl.Search.Models;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fpl.Search.Indexing;

public class SlowEntryIndexProvider : IndexProviderBase, IIndexProvider<EntryItem>, ISingleEntryIndexProvider, IVerifiedEntryIndexProvider
{
    private readonly IEntryClient _entryClient;
    private readonly IEntryHistoryClient _entryHistoryClient;
    private readonly IEntryIndexBookmarkProvider _indexBookmarkProvider;
    private readonly IVerifiedEntriesRepository _verifiedEntriesRepository;
    private readonly ILogger<IndexProviderBase> _logger;
    private readonly SearchOptions _options;
    private VerifiedEntry[] _allVerifiedEntries = Array.Empty<VerifiedEntry>();
    private int _currentConsecutiveCountOfMissingEntries;
    private int _bookmarkCounter;

    public SlowEntryIndexProvider(
        ILeagueClient leagueClient,
        IEntryClient entryClient,
        IEntryHistoryClient entryHistoryClient,
        IEntryIndexBookmarkProvider indexBookmarkProvider,
        IVerifiedEntriesRepository verifiedEntriesRepository,
        ILogger<IndexProviderBase> logger,
        IOptions<SearchOptions> options) : base(leagueClient, logger)
    {
        _entryClient = entryClient;
        _entryHistoryClient = entryHistoryClient;
        _indexBookmarkProvider = indexBookmarkProvider;
        _verifiedEntriesRepository = verifiedEntriesRepository;
        _logger = logger;
        _options = options.Value;
    }

    public string IndexName => _options.EntriesIndex;
    public Task<int> StartIndexingFrom => _indexBookmarkProvider.GetBookmark();

    public async Task Init()
    {
        await RefreshVerifiedEntries();
    }

    private async Task RefreshVerifiedEntries()
    {
        _allVerifiedEntries = (await _verifiedEntriesRepository.GetAllVerifiedEntries()).ToArray();
    }

    public async Task<(EntryItem[], bool)> GetBatchToIndex(int i, int batchSize)
    {
        var entryBatch = await ClientHelper.PolledRequests(() => Enumerable.Range(i, batchSize).Select(n => _entryClient.Get(n, tolerate404: true)).ToArray(), _logger);
        var items = entryBatch
            .Where(x => x != null && x.Exists)
            .Select(y => new EntryItem { Id = y.Id, TeamName = y.TeamName, RealName = y.PlayerFullName, Country = y.PlayerRegionShortIso }).ToArray();

        if (!items.Any())
        {
            _currentConsecutiveCountOfMissingEntries += batchSize;
        }
        else
        {
            var historyBatch = (await ClientHelper.PolledRequests(() => Enumerable.Range(i, batchSize).Select(n => _entryHistoryClient.GetHistory(n, tolerate404: true)).ToArray(), _logger))
                .Where(x => x.HasValue)
                .Select(x => x.Value)
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
                await _indexBookmarkProvider.SetBookmark(1);
            }
            else
            {
                var resetBookmarkTo = i - _options.ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob;
                await _indexBookmarkProvider.SetBookmark(resetBookmarkTo > 1 ? resetBookmarkTo : 1);
            }
        }
        else if (_bookmarkCounter > 50) // Set a bookmark at every 50th batch
        {
            await _indexBookmarkProvider.SetBookmark(i + batchSize);
            _bookmarkCounter = 0;
        }
        else
        {
            _bookmarkCounter++;
        }

        return (items, couldBeMore);
    }

    private bool IsVerifiedEntry(int entryId)
    {
        return _allVerifiedEntries.Any(verifiedEntry => verifiedEntry.EntryId == entryId);
    }

    public async Task<EntryItem> GetSingleEntryToIndex(int entryId)
    {
        await RefreshVerifiedEntries();

        var entry = await _entryClient.Get(entryId);
        var history = (await _entryHistoryClient.GetHistory(entryId))?.entryHistory;

        if (IsVerifiedEntry(entryId))
        {
            var verifiedEntry = _allVerifiedEntries.Single(x => x.EntryId == entryId);
            return ToEntryItem(verifiedEntry, entry, history);
        }

        return new EntryItem
        {
            Id = entry.Id,
            RealName = entry.PlayerFullName,
            TeamName = entry.TeamName,
            Country = entry.PlayerRegionShortIso,
            NumberOfPastSeasons = history != null ? history.SeasonHistory.Count : 0,
            Thumbprint = history != null ? ToEntryThumbprint(history) : string.Empty
        };
    }

    public async Task<EntryItem[]> GetAllVerifiedEntriesToIndex()
    {
        await RefreshVerifiedEntries();

        return (await Task.WhenAll(_allVerifiedEntries.Select(async verifiedEntry =>
        {
            var entry = await _entryClient.Get(verifiedEntry.EntryId);
            var history = (await _entryHistoryClient.GetHistory(verifiedEntry.EntryId))?.entryHistory;
            return ToEntryItem(verifiedEntry, entry, history);
        }))).ToArray();
    }

    private static EntryItem ToEntryItem(VerifiedEntry verifiedEntry, BasicEntry basicEntry, EntryHistory entryHistory)
    {
        return new EntryItem
        {
            Id = verifiedEntry.EntryId,
            RealName = verifiedEntry.FullName,
            TeamName = verifiedEntry.EntryTeamName,
            VerifiedType = verifiedEntry.VerifiedEntryType,
            Alias = verifiedEntry.Alias,
            Description = verifiedEntry.Description,
            Country = basicEntry.PlayerRegionShortIso,
            NumberOfPastSeasons = entryHistory != null ? entryHistory.SeasonHistory.Count : 0,
            Thumbprint = entryHistory != null ? ToEntryThumbprint(entryHistory) : string.Empty
        };
    }

    private static string ToEntryThumbprint(EntryHistory entryHistory)
    {
        return entryHistory.SeasonHistory.Any() ? string.Join(";", entryHistory.SeasonHistory.Take(2).Select(x => $"{x.SeasonName}:{x.Rank}").ToArray()) : string.Empty;
    }
}
