using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class EntryIndexProvider : IndexProviderBase, IIndexProvider<EntryItem>
    {
        private readonly SearchOptions _options;

        public EntryIndexProvider(
            ILeagueClient leagueClient, 
            ILogger<IndexProviderBase> logger, 
            IOptions<SearchOptions> options) : base(leagueClient, logger)
        {
            _options = options.Value;
        }

        public string IndexName => _options.EntriesIndex;
        public Task<int> StartIndexingFrom => Task.FromResult(1);

        public async Task<(EntryItem[], bool)> GetBatchToIndex(int i, int batchSize)
        {
            var batch = await GetBatchOfLeagues(i, batchSize, (client, x) => client.GetClassicLeague(Constants.GlobalOverallLeagueId, x));
            var items = batch.SelectMany(x =>
                x.Standings.Entries.Select(y => 
                    new EntryItem { Id = y.Id, TeamName = y.EntryName, RealName = y.PlayerName, Entry = y.Entry, VerifiedType = GetVerifiedType(y.Entry) })).ToArray();
            var couldBeMore = batch.All(x => x.Standings.HasNext);
            
            return (items, couldBeMore);
        }
        
        private static VerifiedEntryType? GetVerifiedType(int entryId)
        {
            return VerifiedEntries.VerifiedEntriesMap.ContainsKey(entryId)
                ? VerifiedEntries.VerifiedEntriesMap[entryId]
                : (VerifiedEntryType?) null;
        }
    }
}