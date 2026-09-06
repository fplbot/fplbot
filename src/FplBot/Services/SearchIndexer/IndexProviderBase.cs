using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Search.Indexing;

public abstract class IndexProviderBase(ILeagueClient leagueClient, ILogger<IndexProviderBase> logger)
{
    protected Task<ClassicLeague?[]> GetBatchOfLeagues(int i, int batchSize, Func<ILeagueClient, int, Task<ClassicLeague?>> getLeagueByIterator)
    {
        return ClientHelper.PolledRequests(() => Enumerable.Range(i, batchSize).Select(n => getLeagueByIterator(leagueClient, n)).ToArray(), logger);
    }
}
