using Fpl.Client.Models;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests.Formatting
{
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
            var change = Formatter.Change(new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    News = fromNews,
                    Status = PlayerStatuses.Doubtful,
                },
                ToPlayer = new Player
                {
                    News = toNews,
                    Status = PlayerStatuses.Doubtful    
                }
            });
            Assert.Contains(expected, change);
        }

        [Fact]
        public void NoChanceInfoInNews()
        {
            var change = Formatter.Change(new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    News = "some string not containing percentage of playing",
                    Status = PlayerStatuses.Doubtful,    
                },
                ToPlayer = new Player
                {
                    News = "some string not containing percentage of playing",
                    Status = PlayerStatuses.Doubtful, 
                }
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
            var change = Formatter.Change(new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    News = "dontcare",
                    Status = fromStatus,    
                },
                ToPlayer = new Player
                {
                    News = "dontcare",
                    Status = toStatus   
                }
            });
            Assert.Contains(expected, change);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, "50% chance of playing", "News update")]
        public void AllKindsOfDoubtful(string fromNews, string toNews, string expected)
        {
            var change = Formatter.Change(new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    News = fromNews,
                    Status = PlayerStatuses.Doubtful,
                },
                ToPlayer = new Player
                {
                    News = toNews,
                    Status = PlayerStatuses.Doubtful,    
                }
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
            var change = Formatter.Change(new PlayerUpdate
            {
                FromPlayer = null,
                ToPlayer = null
            });
            Assert.Null(change);
        }
        
        [Fact]
        public void ReportsCovid()
        {
            var change = Formatter.Change(new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    News = "dontcare",
                    Status = "dontCare",
                },
                ToPlayer = new Player
                {
                    News = "Self-isolating",
                    Status = "dontCare",    
                }
            });
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

        private PlayerUpdate Doubtful(int from = 25, int to = 50)
        {
            return new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    WebName = $"Dougie Doubter {from}",
                    News = $"Knock - {from}% chance of playing",
                    Status = PlayerStatuses.Doubtful,
                },
                ToPlayer = new Player
                {
                    WebName = $"Dougie Doubter {to}",
                    News = $"Knock - {to}% chance of playing",
                    Status = PlayerStatuses.Doubtful,
                },
                Team = new Team { ShortName = "TestTeam" }
            };
        }
        
        private PlayerUpdate Available()
        {
            return new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    WebName = "Able Availbleu",
                    News = $"Knock - 75% chance of playing",
                    Status = PlayerStatuses.Doubtful,    
                },
                ToPlayer = new Player
                {
                    WebName = "Able Availbleu",
                    News = "",
                    Status = PlayerStatuses.Available,    
                },
                Team = new Team { ShortName = "TestTeam" }
            };
        }
    }
}