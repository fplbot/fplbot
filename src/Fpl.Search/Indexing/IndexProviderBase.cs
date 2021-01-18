using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Search.Indexing
{
    public abstract class IndexProviderBase
    {
        private readonly ILeagueClient _leagueClient;
        private readonly ILogger<IndexProviderBase> _logger;
        private const int RetriesOnErrorBeforeGivingUp = 3;

        protected IndexProviderBase(ILeagueClient leagueClient, ILogger<IndexProviderBase> logger)
        {
            _leagueClient = leagueClient;
            _logger = logger;
        }

        protected async Task<ClassicLeague[]> GetBatchOfLeagues(int i, int batchSize, Func<ILeagueClient, int, Task<ClassicLeague>> getLeagueByIterator)
        {
            var j = 0;
            while (j < RetriesOnErrorBeforeGivingUp)
            {
                try
                {
                    return await Task.WhenAll(Enumerable.Range(i, batchSize).Select(n => getLeagueByIterator(_leagueClient, n)));
                }
                catch (HttpRequestException e)
                {
                    _logger.LogWarning("Ran into a 429 (Too Many Requests) at {i}. Waiting 2s before retrying.", i);
                    await Task.Delay(2000);
                    j++;
                }
            }
            throw new Exception($"Unable to get standings after {RetriesOnErrorBeforeGivingUp} retries");
        }
    }
}