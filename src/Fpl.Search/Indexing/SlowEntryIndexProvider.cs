using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Search.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Models;

namespace Fpl.Search.Indexing
{
    public class SlowEntryIndexProvider : IndexProviderBase, IIndexProvider<EntryItem>, ISingleEntryIndexProvider, IVerifiedEntryIndexProvider
    {
        private readonly IEntryClient _entryClient;
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
            IEntryIndexBookmarkProvider indexBookmarkProvider,
            IVerifiedEntriesRepository verifiedEntriesRepository,
            ILogger<IndexProviderBase> logger,
            IOptions<SearchOptions> options) : base(leagueClient, logger)
        {
            _entryClient = entryClient;
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
            var batch = await ClientHelper.PolledRequests(() => Enumerable.Range(i, batchSize).Select(n => _entryClient.Get(n, tolerate404: true)).ToArray(), _logger);
            var items = batch
                .Where(x => x != null && x.Exists)
                .Select(y => new EntryItem { Id = y.Id, TeamName = y.TeamName, RealName = y.PlayerFullName }).ToArray();

            if (!items.Any())
            {
                _currentConsecutiveCountOfMissingEntries += batchSize;
            }
            else
            {
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

            if (IsVerifiedEntry(entryId))
            {
                return ToEntryItem(_allVerifiedEntries.Single(x => x.EntryId == entryId));
            }

            var entry = await _entryClient.Get(entryId);
            return new EntryItem {Id = entry.Id, RealName = entry.PlayerFullName, TeamName = entry.TeamName};
        }

        public async Task<EntryItem[]> GetAllVerifiedEntriesToIndex()
        {
            await RefreshVerifiedEntries();

            return _allVerifiedEntries.Select(ToEntryItem).ToArray();
        }

        private static EntryItem ToEntryItem(VerifiedEntry entry)
        {
            return new EntryItem
            {
                Id = entry.EntryId,
                RealName = entry.FullName,
                TeamName = entry.EntryTeamName,
                VerifiedType = entry.VerifiedEntryType,
                Alias = entry.Alias,
                Description = entry.Description
            };
        }
    }
}
