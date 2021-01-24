using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public abstract class IndexProviderBase
    {
        private readonly ILeagueClient _leagueClient;
        private readonly ILogger<IndexProviderBase> _logger;

        protected IndexProviderBase(ILeagueClient leagueClient, ILogger<IndexProviderBase> logger)
        {
            _leagueClient = leagueClient;
            _logger = logger;
        }

        protected Task<ClassicLeague[]> GetBatchOfLeagues(int i, int batchSize, Func<ILeagueClient, int, Task<ClassicLeague>> getLeagueByIterator)
        {
            return ClientHelper.PolledRequests(Enumerable.Range(i, batchSize).Select(n => getLeagueByIterator(_leagueClient, n)).ToArray(), _logger);
        }
    }
}