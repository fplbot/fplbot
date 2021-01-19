using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public class LeagueIndexProvider : IndexProviderBase, IIndexProvider<LeagueItem>
    {
        private readonly SearchOptions _options;

        public LeagueIndexProvider(ILeagueClient leagueClient, ILogger<IndexProviderBase> logger, IOptions<SearchOptions> options) : base(leagueClient, logger)
        {
            _options = options.Value;
        }

        public string IndexName => _options.LeaguesIndex;

        public async Task<(LeagueItem[], bool)> GetBatchToIndex(int i, int batchSize)
        {
            var batch = await GetBatchOfLeagues(i, batchSize, (client, x) => client.GetClassicLeague(x, tolerate404: true));
            var items = batch
                .Where(x => x != null)
                .Select(x => new LeagueItem { Id = x.Properties.Id, Name = x.Properties.Name, AdminEntry = x.Properties.AdminEntry })
                .ToArray();
            var couldBeMore = batch.Count(x => x.Exists) > 1;

            return (items, couldBeMore);
        }
    }
}