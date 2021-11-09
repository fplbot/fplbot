using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fpl.Search.Indexing;

public class EntryIndexProvider : IndexProviderBase, IIndexProvider<EntryItem>, ISingleEntryIndexProvider, IVerifiedEntryIndexProvider
{
    private readonly IEntryClient _entryClient;
    private readonly IVerifiedEntriesRepository _verifiedEntriesRepository;
    private readonly SearchOptions _options;
    private VerifiedEntry[] _allVerifiedEntries = Array.Empty<VerifiedEntry>();

    public EntryIndexProvider(
        ILeagueClient leagueClient,
        IEntryClient entryClient,
        IVerifiedEntriesRepository verifiedEntriesRepository,
        ILogger<IndexProviderBase> logger,
        IOptions<SearchOptions> options) : base(leagueClient, logger)
    {
        _entryClient = entryClient;
        _verifiedEntriesRepository = verifiedEntriesRepository;
        _options = options.Value;
    }

    public string IndexName => _options.EntriesIndex;
    public Task<int> StartIndexingFrom => Task.FromResult(1);

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
        var batch = await GetBatchOfLeagues(i, batchSize, (client, x) => client.GetClassicLeague(Constants.GlobalOverallLeagueId, x));
        var items = batch.SelectMany(x =>
            x.Standings.Entries
                .Where(entry => !IsVerifiedEntry(entry.Entry))
                .Select(y => new EntryItem { Id = y.Entry, TeamName = y.EntryName, RealName = y.PlayerName })).ToArray();
        var couldBeMore = batch.All(x => x.Standings.HasNext);

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

public interface ISingleEntryIndexProvider
{
    string IndexName { get; }
    Task<EntryItem> GetSingleEntryToIndex(int entryId);
}

public interface IVerifiedEntryIndexProvider
{
    string IndexName { get; }
    Task<EntryItem[]> GetAllVerifiedEntriesToIndex();
}