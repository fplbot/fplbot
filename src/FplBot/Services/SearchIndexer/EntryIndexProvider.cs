using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Options;

namespace Fpl.Search.Indexing;

public class EntryIndexProvider(
    ILeagueClient leagueClient,
    IEntryClient entryClient,
    ILogger<IndexProviderBase> logger,
    IOptions<SearchOptions> options)
    : IndexProviderBase(leagueClient, logger), IIndexProvider<EntryItem>, ISingleEntryIndexProvider
{
    private readonly SearchOptions _options = options.Value;

    public string IndexName => _options.EntriesIndex;
    public Task<int> StartIndexingFrom => Task.FromResult(1);

    public Task Init() => Task.CompletedTask;

    public async Task<(EntryItem[], bool)> GetBatchToIndex(int i, int batchSize)
    {
        var batch = await GetBatchOfLeagues(i, batchSize, (client, x) => client.GetClassicLeague(Constants.GlobalOverallLeagueId, x));
        var validBatch = batch.Where(x => x != null).Select(x => x!).ToArray();
        var items = validBatch.SelectMany(x =>
            (x.Standings?.Entries ?? Enumerable.Empty<Fpl.Client.Models.ClassicLeagueEntry>())
                .Select(y => new EntryItem { Id = y.Entry, TeamName = y.EntryName, RealName = y.PlayerName })).ToArray();
        var couldBeMore = validBatch.All(x => x.Standings?.HasNext == true);

        return (items, couldBeMore);
    }

    public async Task<EntryItem?> GetSingleEntryToIndex(int entryId)
    {
        var entry = await entryClient.Get(entryId);
        return entry == null ? null : new EntryItem { Id = entry.Id, RealName = entry.PlayerFullName, TeamName = entry.TeamName };
    }
}

public interface ISingleEntryIndexProvider
{
    string IndexName { get; }
    Task<EntryItem?> GetSingleEntryToIndex(int entryId);
}
