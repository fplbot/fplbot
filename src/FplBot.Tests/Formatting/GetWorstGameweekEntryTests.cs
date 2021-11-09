using Fpl.Client.Models;
using FplBot.Formatting;

namespace FplBot.Tests.Formatting;

public class GetWorstGameweekEntryTests
{
    [Fact]
    public void GetWorstGameweekEntryTests_ShoudReturnCorrectMessage()
    {
        // Arrange
        var gameweek = new Gameweek
        {
            Name = "1",
            Id = 1
        };
        var league = new ClassicLeague
        {
            Standings = new ClassicLeagueStandings
            {
                Entries = new[]
                {
                    new ClassicLeagueEntry
                    {
                        EventTotal = 50,
                        Total = 1337,
                        Entry = 1,
                        EntryName = "K"
                    },
                    new ClassicLeagueEntry
                    {
                        EventTotal = 90,
                        Total = 500,
                        Entry = 2,
                        EntryName = "L"
                    },
                    new ClassicLeagueEntry
                    {
                        EventTotal = 90,
                        Total = 500,
                        Entry = 3,
                        EntryName = "La"
                    },
                    new ClassicLeagueEntry
                    {
                        EventTotal = 10,
                        Total = 42,
                        Entry = 4,
                        EntryName = "M"
                    },
                    new ClassicLeagueEntry
                    {
                        EventTotal = 10,
                        Total = 43,
                        Entry = 5,
                        EntryName = "J"
                    },
                    new ClassicLeagueEntry
                    {
                        EventTotal = 5,
                        Total = 43,
                        Entry = 6,
                        EntryName = "X"
                    }
                }
            }
        };

        // Act
        var message = Formatter.GetWorstGameweekEntry(league, gameweek);

        // Assert
        Assert.Equal("💩 <https://fantasy.premierleague.com/entry/6/event/1|X> only got 5 points. Wow.", message);
    }
}