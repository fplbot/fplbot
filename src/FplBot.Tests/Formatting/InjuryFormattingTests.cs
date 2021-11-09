using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Events.v1;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests.Formatting;

public class InjuryFormattingTests
{
    private readonly ITestOutputHelper _helper;

    public InjuryFormattingTests(ITestOutputHelper helper)
    {
        _helper = helper;
    }

    [Theory]
    [InlineData("50% chance of playing", "75% chance of playing", "Increased chance of playing")]
    [InlineData("75% chance of playing", "50% chance of playing", "Decreased chance of playing")]
    public void IncreaseChanceOfPlaying(string fromNews, string toNews, string expected)
    {
        var change = Formatter.Change(new InjuredPlayerUpdate(

            new InjuredPlayer(1,"WebName", 13, new TeamDescription(1, "TEA", "Team United")),
            new InjuryStatus(
                PlayerStatuses.Doubtful,
                fromNews
            ),
            new InjuryStatus(
                PlayerStatuses.Doubtful,
                toNews
            )
        ));
        Assert.Contains(expected, change);
    }

    [Fact]
    public void NoChanceInfoInNews()
    {
        var change = Formatter.Change(new InjuredPlayerUpdate(

            new InjuredPlayer(1,"WebName", 13, new TeamDescription(1, "TEA", "Team United")),
            new InjuryStatus(
                PlayerStatuses.Doubtful,
                "some string not containing percentage of playing"
            ),
            new InjuryStatus(
                PlayerStatuses.Doubtful,
                "some string not containing percentage of playing"
            )
        ));
        Assert.Null(change);
    }

    [Theory]
    [InlineData(PlayerStatuses.Doubtful, PlayerStatuses.Available, "Available")]
    [InlineData(PlayerStatuses.Injured, PlayerStatuses.Available, "Available")]
    [InlineData(PlayerStatuses.Suspended, PlayerStatuses.Available, "Available")]
    [InlineData(PlayerStatuses.Unavailable, PlayerStatuses.Available, "Available")]
    [InlineData(PlayerStatuses.NotInSquad, PlayerStatuses.Available, "Available")]
    [InlineData(PlayerStatuses.Available, PlayerStatuses.Doubtful, "Doubtful")]
    public void TestSuite(string fromStatus, string toStatus, string expected)
    {
        var change = Formatter.Change(new InjuredPlayerUpdate(

            new InjuredPlayer(1,"WebName", 13, new TeamDescription(1, "TEA", "Team United")),
            new InjuryStatus(
                fromStatus,
                "dontcare"
            ),
            new InjuryStatus(
                toStatus,
                "dontcare"
            )
        ));
        Assert.Contains(expected, change);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, "50% chance of playing", "News update")]
    public void AllKindsOfDoubtful(string fromNews, string toNews, string expected)
    {
        var change = Formatter.Change(new InjuredPlayerUpdate(

            new InjuredPlayer(1,"WebName", 13, new TeamDescription(1, "TEA", "Team United")),
            new InjuryStatus(
                PlayerStatuses.Doubtful,
                fromNews
            ),
            new InjuryStatus(
                PlayerStatuses.Doubtful,
                toNews
            )
        ));

        if (expected == null)
        {
            Assert.Null(change);
        }
        else
        {
            Assert.Contains(expected, change);
        }

    }

    [Fact]
    public void ErronousUpdateHandling()
    {
        var change = Formatter.Change(new InjuredPlayerUpdate(null, null, null));
        Assert.Null(change);
    }

    [Fact]
    public void ReportsCovid()
    {
        var change = Formatter.Change(new InjuredPlayerUpdate(

            new InjuredPlayer(1,"WebName", 13, new TeamDescription(1, "TEA", "Team United")),
            new InjuryStatus(
                "dontCareStatus",
                "dontCareNews"
            ),
            new InjuryStatus(
                "dontCareStatus",
                "Self-isolating"
            )
        ));

        Assert.Contains("🦇", change);
    }

    [Fact]
    public void MultipleTests()
    {
        var formatted = Formatter.FormatInjuryStatusUpdates(new[]
        {
            Doubtful(25, 50),
            Doubtful(75,25),
            Available()
        });
        _helper.WriteLine(formatted);
    }

    private InjuredPlayerUpdate Doubtful(int from = 25, int to = 50)
    {
        return new InjuredPlayerUpdate
        (
            new InjuredPlayer(1, $"Dougie Doubter {from}",13,new TeamDescription(1,"TEA", "TEAM UTD")),
            new InjuryStatus(PlayerStatuses.Doubtful,$"Knock - {from}% chance of playing"),
            new InjuryStatus(PlayerStatuses.Doubtful,$"Knock - {to}% chance of playing")
        );
    }

    private InjuredPlayerUpdate Available()
    {
        return new InjuredPlayerUpdate
        (
            new InjuredPlayer(2, $"Able Availbleu",13, new TeamDescription(1,"TEA", "TEAM UTD")),
            new InjuryStatus(PlayerStatuses.Doubtful,$"Knock - 1337% chance of playing"),
            new InjuryStatus(PlayerStatuses.Available,"")
        );
    }
}