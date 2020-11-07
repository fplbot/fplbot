using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class PlayerStatusUpdateTests
    {
        private readonly ITestOutputHelper _helper;

        public PlayerStatusUpdateTests(ITestOutputHelper helper)
        {
            _helper = helper;
        }
        
        [Theory]
        [InlineData("50% chance of playing", "75% chance of playing", "Increased chance of playing")]
        [InlineData("75% chance of playing", "50% chance of playing", "Decreased chance of playing")]
        public void IncreaseChanceOfPlaying(string fromNews, string toNews, string expected)
        {
            var change = Formatter.Change(new PlayerStatusUpdate
            {
                FromNews = fromNews,
                FromStatus = PlayerStatuses.Doubtful,
                ToNews = toNews,
                ToStatus = PlayerStatuses.Doubtful, 
            });
            Assert.Contains(expected, change);
        }

        [Fact]
        public void NoChanceInfoInNews()
        {
            var change = Formatter.Change(new PlayerStatusUpdate
            {
                FromNews = "some string not containing percentage of playing",
                FromStatus = PlayerStatuses.Doubtful,
                ToNews = "some string not containing percentage of playing",
                ToStatus = PlayerStatuses.Doubtful, 
            });
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
            var change = Formatter.Change(new PlayerStatusUpdate
            {
                FromNews = "dontcare",
                FromStatus = fromStatus,
                ToNews = "dontcare",
                ToStatus = toStatus, 
            });
            Assert.Contains(expected, change);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, "50% chance of playing", "News update")]
        public void AllKindsOfDoubtful(string fromNews, string toNews, string expected)
        {
            var change = Formatter.Change(new PlayerStatusUpdate
            {
                FromNews = fromNews,
                FromStatus = PlayerStatuses.Doubtful,
                ToNews = toNews,
                ToStatus = PlayerStatuses.Doubtful 
            });
            
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
            var change = Formatter.Change(new PlayerStatusUpdate
            {
                FromNews = null,
                FromStatus = null,
                ToNews = null,
                ToStatus = null
            });
            Assert.Null(change);
        }
        
        [Fact]
        public void ReportsCovid()
        {
            var change = Formatter.Change(new PlayerStatusUpdate
            {
                FromNews = "dontcare",
                FromStatus = "dontCare",
                ToNews = "Self-isolating",
                ToStatus = "dontCare", 
            });
            Assert.Contains("🦇", change);
        }

        [Fact]
        public void MultipleTests()
        {
            var formatted = Formatter.FormatStatusUpdates(new[]
            {
                Doubtful(25, 50),
                Doubtful(75,25),
                Available()
            });
            _helper.WriteLine(formatted);
        }

        private PlayerStatusUpdate Doubtful(int from = 25, int to = 50)
        {
            return new PlayerStatusUpdate
            {
                PlayerWebName = $"Dougie Doubter {to}",
                FromNews = $"Knock - {from}% chance of playing",
                FromStatus = PlayerStatuses.Doubtful,
                ToNews = $"Knock - {to}% chance of playing",
                ToStatus = PlayerStatuses.Doubtful,
                TeamName = "TestTeam"
            };
        }
        
        private PlayerStatusUpdate Available()
        {
            return new PlayerStatusUpdate
            {
                PlayerWebName = "Able Availbleu",
                FromNews = $"Knock - 75% chance of playing",
                FromStatus = PlayerStatuses.Doubtful,
                ToNews = "",
                ToStatus = PlayerStatuses.Available,
                TeamName = "TestTeam"
            };
        }
    }
}