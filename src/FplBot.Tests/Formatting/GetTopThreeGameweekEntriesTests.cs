using Fpl.Client.Models;
using FplBot.Formatting;
using Xunit;

namespace FplBot.Tests.Formatting;

public class GetTopThreeGameweekEntriesTests
{
    [Fact]
    public void GetTopThreeGameweekEntriesTests_ShoudReturnCorrectMessage()
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
        var message = Formatter.GetTopThreeGameweekEntries(league, gameweek);

        // Assert
        Assert.Equal("Top three this gameweek was:\n" +
                     "🥇 <https://fantasy.premierleague.com/entry/2/event/1|L> - 90\n" +
                     "🥇 <https://fantasy.premierleague.com/entry/3/event/1|La> - 90\n" +
                     "🥈 <https://fantasy.premierleague.com/entry/1/event/1|K> - 50\n" +
                     "🥉 <https://fantasy.premierleague.com/entry/4/event/1|M> - 10\n" +
                     "🥉 <https://fantasy.premierleague.com/entry/5/event/1|J> - 10\n", message);
    }
}