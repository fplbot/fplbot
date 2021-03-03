using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Data.Repositories;

namespace Fpl.Search.Indexing
{
    public class LeagueIndexProvider : IndexProviderBase, IIndexProvider<LeagueItem>
    {
        private readonly IEntryClient _entryClient;
        private readonly IIndexBookmarkProvider _indexBookmarkProvider;
        private readonly ILogger<IndexProviderBase> _logger;
        private readonly SearchOptions _options;
        private int _currentConsecutiveCountOfMissingLeagues;
        private int _bookmarkCounter;

        public LeagueIndexProvider(
            ILeagueClient leagueClient,
            IEntryClient entryClient,
            IIndexBookmarkProvider indexBookmarkProvider,
            ILogger<IndexProviderBase> logger,
            IOptions<SearchOptions> options) : base(leagueClient, logger)
        {
            _entryClient = entryClient;
            _indexBookmarkProvider = indexBookmarkProvider;
            _logger = logger;
            _options = options.Value;
        }

        public string IndexName => _options.LeaguesIndex;
        public Task<int> StartIndexingFrom => _indexBookmarkProvider.GetBookmark();

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public async Task<(LeagueItem[], bool)> GetBatchToIndex(int i, int batchSize)
        {
            var batch = await GetBatchOfLeagues(i, batchSize,
                (client, x) => client.GetClassicLeague(x, tolerate404: true));
            var items = batch
                .Where(x => x != null && x.Exists)
                .Select(x => new LeagueItem
                {
                    Id = x.Properties.Id, Name = x.Properties.Name, AdminEntry = x.Properties.AdminEntry
                })
                .ToArray();

            var adminsToFetch = items.Where(x => x.AdminEntry != null).Select(x => x.AdminEntry.Value).Distinct()
                .ToArray();

            if (adminsToFetch.Any())
            {
                var admins =
                    await ClientHelper.PolledRequests(() => adminsToFetch.Select(x => _entryClient.Get(x)).ToArray(),
                        _logger);
                foreach (var leagueItem in items)
                {
                    var admin = admins.SingleOrDefault(a => a.Id == leagueItem.AdminEntry);
                    if (admin != null)
                    {
                        leagueItem.AdminName = admin.PlayerFullName;
                        leagueItem.AdminTeamName = admin.TeamName;
                        leagueItem.AdminCountry = admin.PlayerRegionShortIso;
                    }
                }
            }

            if (!items.Any())
            {
                _currentConsecutiveCountOfMissingLeagues += batchSize;
            }
            else
            {
                _currentConsecutiveCountOfMissingLeagues = 0;
            }

            // There are large "gaps" of missing leagues (deleted ones, perhaps). The indexing job needs to work its way past these gaps, but still stop when
            // we think that there are none left to index
            var couldBeMore = _currentConsecutiveCountOfMissingLeagues <
                              _options.ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob;

            if (!couldBeMore)
            {
                await _indexBookmarkProvider.SetBookmark(1);
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
    }
}
