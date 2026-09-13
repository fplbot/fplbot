using FakeItEasy;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search;
using Fpl.Search.Data.Abstractions;
using Fpl.Search.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FplBot.Tests.SearchIndexer;

public class SlowEntryIndexProviderTests
{
    private readonly ILeagueClient _leagueClient = A.Fake<ILeagueClient>();
    private readonly IEntryClient _entryClient = A.Fake<IEntryClient>();
    private readonly IEntryHistoryClient _entryHistoryClient = A.Fake<IEntryHistoryClient>();
    private readonly IEntryIndexBookmarkProvider _bookmarkProvider = A.Fake<IEntryIndexBookmarkProvider>();
    private readonly SlowEntryIndexProvider _sut;

    public SlowEntryIndexProviderTests()
    {
        _sut = new SlowEntryIndexProvider(
            _leagueClient,
            _entryClient,
            _entryHistoryClient,
            _bookmarkProvider,
            NullLogger<IndexProviderBase>.Instance,
            Options.Create(new SearchOptions
            {
                IndexUri = "uri",
                Username = "u",
                Password = "p",
                EntriesIndex = "entries",
                LeaguesIndex = "leagues",
                AnalyticsIndex = "analytics",
                IndexingCron = "* * * * *",
                ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob = 10,
                ResetIndexingBookmarkWhenDone = false
            }));
    }

    [Fact]
    public async Task GetBatchToIndex_EntryExistsButHasNoHistory_IndexesEntryWithoutThrowing()
    {
        A.CallTo(() => _entryClient.Get(1, true)).Returns(new BasicEntry { Id = 1, TeamName = "Team A", PlayerFirstName = "John", PlayerLastName = "Doe" });
        A.CallTo(() => _entryClient.Get(A<int>.That.Not.IsEqualTo(1), true)).Returns((BasicEntry?)null);
        A.CallTo(() => _entryHistoryClient.GetHistory(A<int>._, true)).Returns(((int, EntryHistory)?)null);

        var (items, _) = await _sut.GetBatchToIndex(1, 8);

        var entry = Assert.Single(items);
        Assert.Equal(1, entry.Id);
        Assert.Equal(0, entry.NumberOfPastSeasons);
        Assert.Null(entry.Thumbprint);
    }
}
