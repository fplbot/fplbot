using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fpl.Search.Indexing;

public class EntryIndexProvider : IndexProviderBase, IIndexProvider<EntryItem>, ISingleEntryIndexProvider
{
    private readonly IEntryClient _entryClient;
    private readonly SearchOptions _options;

    public EntryIndexProvider(
        ILeagueClient leagueClient,
        IEntryClient entryClient,
        ILogger<IndexProviderBase> logger,
        IOptions<SearchOptions> options) : base(leagueClient, logger)
    {
        _entryClient = entryClient;
        _options = options.Value;
    }

    public string IndexName => _options.EntriesIndex;
    public Task<int> StartIndexingFrom => Task.FromResult(1);

    public Task Init() => Task.CompletedTask;

    public async Task<(EntryItem[], bool)> GetBatchToIndex(int i, int batchSize)
    {
        var batch = await GetBatchOfLeagues(i, batchSize, (client, x) => client.GetClassicLeague(Constants.GlobalOverallLeagueId, x));
        var items = batch.SelectMany(x =>
            x.Standings.Entries
                .Select(y => new EntryItem { Id = y.Entry, TeamName = y.EntryName, RealName = y.PlayerName })).ToArray();
        var couldBeMore = batch.All(x => x.Standings.HasNext);

        return (items, couldBeMore);
    }

    public async Task<EntryItem> GetSingleEntryToIndex(int entryId)
    {
        var entry = await _entryClient.Get(entryId);
        return new EntryItem { Id = entry.Id, RealName = entry.PlayerFullName, TeamName = entry.TeamName };
    }
}

public interface ISingleEntryIndexProvider
{
    string IndexName { get; }
    Task<EntryItem> GetSingleEntryToIndex(int entryId);
}
