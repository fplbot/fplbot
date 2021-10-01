using Fpl.Client.Models;
using FplBot.Slack.Helpers.Formatting;
using Xunit;

namespace FplBot.Tests.Formatting
{
    public class FormatGameweekFinishedTests
    {
        [Theory]
        [InlineData(30, 10, 11, 11, "Gameweek 1 is finished. It was probably a disappointing one, with a global average of *30* points. I'm afraid your league did worse than this, with your *11* points average.")]
        [InlineData(50, 10, 11, 11, "Gameweek 1 is finished. The global average was *50* points. I'm afraid your league did worse than this, with your *11* points average.")]
        [InlineData(90, 10, 11, 11, "Gameweek 1 is finished. Must've been pretty intense, with a global average of *90* points. I'm afraid your league did worse than this, with your *11* points average.")]
        [InlineData(30, 50, 40, 45, "Gameweek 1 is finished. It was probably a disappointing one, with a global average of *30* points. Your league did better than this, though - with *45* points average.")]
        [InlineData(50, 50, 40, 45, "Gameweek 1 is finished. The global average was *50* points. I'm afraid your league did slightly worse than this, with your *45* points average.")]
        [InlineData(90, 90, 91, 91, "Gameweek 1 is finished. Must've been pretty intense, with a global average of *90* points. Your league did slightly better than this, though - with *91* points average.")]
        [InlineData(90, 90, 90, 91, "Gameweek 1 is finished. Must've been pretty intense, with a global average of *90* points. I guess your league is pretty mediocre, since you got the exact same *90* points average.")]
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
}
