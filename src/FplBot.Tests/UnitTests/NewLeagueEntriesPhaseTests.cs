using Fpl.Client.Models;
using FplBot.EventHandlers;

namespace FplBot.Tests.UnitTests;

public class NewLeagueEntriesPhaseTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    [InlineData(5, 3)]
    [InlineData(6, 4)]
    [InlineData(9, 4)]
    public void MapsGameweekToTheMonthPhaseContainingIt(int gameweek, int expectedPhase)
    {
        Assert.Equal(expectedPhase, NewLeagueEntriesLookup.ToPhase(Settings(), gameweek));
    }

    [Fact]
    public void NeverPicksOverall_EvenThoughItSpansEveryGameweek()
    {
        foreach (var gameweek in Enumerable.Range(1, 9))
        {
            Assert.NotEqual(1, NewLeagueEntriesLookup.ToPhase(Settings(), gameweek));
        }
    }

    [Fact]
    public void WhenNoPhaseCoversTheGameweek_SendsNoPhaseAtAll()
    {
        Assert.Null(NewLeagueEntriesLookup.ToPhase(Settings(), 38));
        Assert.Null(NewLeagueEntriesLookup.ToPhase(null, 4));
    }

    private static GlobalSettings Settings() => new()
    {
        Phases =
        [
            new Phase { Id = 1, Name = "Overall", StartEvent = 1, StopEvent = 38 },
            new Phase { Id = 2, Name = "August", StartEvent = 1, StopEvent = 2 },
            new Phase { Id = 3, Name = "September", StartEvent = 3, StopEvent = 5 },
            new Phase { Id = 4, Name = "October", StartEvent = 6, StopEvent = 9 }
        ]
    };
}
