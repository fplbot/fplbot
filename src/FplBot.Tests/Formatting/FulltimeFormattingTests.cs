using Fpl.Client.Models;
using FplBot.Core.Handlers.FplEvents;
using FplBot.Core.Helpers;
using FplBot.Core.Helpers.Formatting;
using FplBot.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests.Formatting
{
    public class FulltimeFormattingTests
    {
        private readonly ITestOutputHelper _helper;

        public FulltimeFormattingTests(ITestOutputHelper helper)
        {
            _helper = helper;
        }

        [Fact]
        public void Distributed()
        {
            _helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
            {
                BonusPointsPlayer("player-E", 10),
                BonusPointsPlayer("player-D", 20),
                BonusPointsPlayer("player-C", 30),
                BonusPointsPlayer("player-B", 40),
                BonusPointsPlayer("player-A", 50)
            })));
        }

        [Fact]
        public void SharedFirstPlace()
        {
            _helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
            {
                BonusPointsPlayer("player-E", 10),
                BonusPointsPlayer("player-D", 20),
                BonusPointsPlayer("player-C", 30),
                BonusPointsPlayer("player-B", 40),
                BonusPointsPlayer("player-A", 40)
            })));
        }

        [Fact]
        public void AllSharedFirstPlace()
        {
            _helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
            {
                BonusPointsPlayer("player-E", 10),
                BonusPointsPlayer("player-D", 20),
                BonusPointsPlayer("player-C", 40),
                BonusPointsPlayer("player-B", 40),
                BonusPointsPlayer("player-A", 40)
            })));
        }

        [Fact]
        public void TiedSecondPlace()
        {
            _helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
            {
                BonusPointsPlayer("player-E", 10),
                BonusPointsPlayer("player-D", 20),
                BonusPointsPlayer("player-C", 30),
                BonusPointsPlayer("player-B", 30),
                BonusPointsPlayer("player-A", 40)
            })));
        }

        [Fact]
        public void TiedSecondPlaceForThreePlayers()
        {
            _helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
            {
                BonusPointsPlayer("player-E", 10),
                BonusPointsPlayer("player-D", 30),
                BonusPointsPlayer("player-C", 30),
                BonusPointsPlayer("player-B", 30),
                BonusPointsPlayer("player-A", 40)
            })));
        }

        [Fact]
        public void TiedThirdPlace()
        {
            _helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
            {
                BonusPointsPlayer("player-E", 10),
                BonusPointsPlayer("player-D", 20),
                BonusPointsPlayer("player-C", 20),
                BonusPointsPlayer("player-B", 30),
                BonusPointsPlayer("player-A", 40)
            })));
        }

        [Fact]
        public void TiedThirdPlaceForMultiplePlayers()
        {
            _helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
            {
                BonusPointsPlayer("player-E", 20),
                BonusPointsPlayer("player-D", 20),
                BonusPointsPlayer("player-C", 20),
                BonusPointsPlayer("player-B", 30),
                BonusPointsPlayer("player-A", 40)
            })));
        }

        private FinishedFixture GetProvisionalFinishedFixture(params BonusPointsPlayer[] bonusPointsPlayers)
        {
            return new FinishedFixture
                {
                    Fixture = TestBuilder.AwayTeamGoal(1, 1),
                    HomeTeam = TestBuilder.HomeTeam(),
                    AwayTeam = TestBuilder.AwayTeam(),
                    BonusPoints = bonusPointsPlayers
                }
            ;
        }
        BonusPointsPlayer BonusPointsPlayer(string webName, int bonusPoints)
        {
            return new BonusPointsPlayer
            {
                Player = new Player { WebName = webName},
                BonusPoints = bonusPoints
            };
        }
    }
}
