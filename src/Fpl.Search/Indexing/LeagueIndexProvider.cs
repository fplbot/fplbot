using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class LeagueIndexProvider : IndexProviderBase, IIndexProvider<LeagueItem>
    {
        public LeagueIndexProvider(ILeagueClient leagueClient, ILogger<IndexProviderBase> logger) : base(leagueClient, logger)
        {
        }

        public string IndexName => Constants.LeaguesIndex;

        public async Task<(LeagueItem[], bool)> GetBatchToIndex(int i, int batchSize)
        {
            var batch = await GetBatchOfLeagues(i, batchSize, (client, x) => client.GetClassicLeague(x));
            var items = batch.Select(x => new LeagueItem { Id = x.Properties.Id, Name = x.Properties.Name, AdminEntry = x.Properties.AdminEntry }).ToArray();
            var couldBeMore = batch.Count(x => x.Exists) > 1;

            return (items, couldBeMore);
        }
    }
}