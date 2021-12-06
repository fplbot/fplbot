using Fpl.Client.Models;
using FplBot.Formatting;

namespace FplBot.Tests.Formatting;

public class FormatGameweekFinishedTests
{
    [Theory]
    [InlineData(30, 10, 11, 11, "Gameweek 1 is finished. It was probably a disappointing one, with a global average of *30* points. Your league's average was *11* points.")]
    [InlineData(50, 10, 11, 11, "Gameweek 1 is finished. The global average was *50* points. Your league's average was *11* points.")]
    [InlineData(90, 10, 11, 11, "Gameweek 1 is finished. Must've been pretty intense, with a global average of *90* points. Your league's average was *11* points.")]
    [InlineData(30, 50, 40, 45, "Gameweek 1 is finished. It was probably a disappointing one, with a global average of *30* points. Your league's average was *45* points.")]
    [InlineData(50, 50, 40, 45, "Gameweek 1 is finished. The global average was *50* points. Your league's average was *45* points.")]
    [InlineData(90, 90, 91, 91, "Gameweek 1 is finished. Must've been pretty intense, with a global average of *90* points. Your league's average was *91* points.")]
    [InlineData(90, 90, 90, 91, "Gameweek 1 is finished. Must've been pretty intense, with a global average of *90* points. Your league's average was *90* points.")]
    public void FormatGameweekFinished_ShoudReturnCorrectMessage(
        int globalAverage, int entryScore1, int entryScore2, int entryScore3, string expectedMessage)
    {
        // Arrange
        var gameweek = new Gameweek
        {
            Name = "Gameweek 1",
            AverageScore = globalAverage
        };
        var league = new ClassicLeague
        {
            Standings = new ClassicLeagueStandings
            {
                Entries = new[]
                {
                    new ClassicLeagueEntry
                    {
                        EventTotal = entryScore1
                    },
                    new ClassicLeagueEntry
                    {
                        EventTotal = entryScore2
                    },
                    new ClassicLeagueEntry
                    {
                        EventTotal = entryScore3
                    }
                }
            }
        };

        // Act
        var message = Formatter.FormatGameweekFinished(gameweek, league);

        // Assert
        Assert.Equal(expectedMessage, message);
    }
}
