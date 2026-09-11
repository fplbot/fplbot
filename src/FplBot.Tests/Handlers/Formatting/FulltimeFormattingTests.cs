using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.Formatting;

public class FulltimeFormattingTests(ITestOutputHelper helper)
{
    [Fact]
    public void Distributed()
    {
        helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
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
        helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
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
        helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
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
        helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
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
        helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
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
        helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
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
        helper.WriteLine(Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture(new[]
        {
            BonusPointsPlayer("player-E", 20),
            BonusPointsPlayer("player-D", 20),
            BonusPointsPlayer("player-C", 20),
            BonusPointsPlayer("player-B", 30),
            BonusPointsPlayer("player-A", 40)
        })));
    }

    [Fact]
    public void DefensiveContributionsOnly_ListsPlayersByContributionsDescending()
    {
        var fixture = GetProvisionalFinishedFixture();
        fixture.DefensiveContributions =
        [
            DefensiveContributionPlayer("player-B", 12),
            DefensiveContributionPlayer("player-A", 10),
            DefensiveContributionPlayer("player-C", 14)
        ];

        var output = Formatter.FormatProvisionalFinished(fixture);
        helper.WriteLine(output);

        Assert.DoesNotContain("Bonus points:", output);
        Assert.Contains("Defensive contributions:", output);
        Assert.Equal(["player-C (14)", "player-B (12)", "player-A (10)"], Formatter.CreateDefensiveContributionsOutput(fixture));
    }

    [Fact]
    public void BonusAndDefensiveContributions_BonusComesFirst()
    {
        var fixture = GetProvisionalFinishedFixture(
            BonusPointsPlayer("player-C", 30),
            BonusPointsPlayer("player-B", 40),
            BonusPointsPlayer("player-A", 50));
        fixture.DefensiveContributions = [DefensiveContributionPlayer("player-D", 11)];

        var output = Formatter.FormatProvisionalFinished(fixture);
        helper.WriteLine(output);

        var bonusIndex = output.IndexOf("Bonus points:", StringComparison.Ordinal);
        var dcIndex = output.IndexOf("Defensive contributions:", StringComparison.Ordinal);
        Assert.True(bonusIndex >= 0);
        Assert.True(dcIndex > bonusIndex);
        Assert.Contains("▪️ player-D (11)", output);
        Assert.Contains("▪️ 1p player-C\n\nDefensive contributions:", output);
    }

    [Fact]
    public void DefensiveContributionsOnly_DoesNotStartWithBlankLine()
    {
        var fixture = GetProvisionalFinishedFixture();
        fixture.DefensiveContributions = [DefensiveContributionPlayer("player-A", 12)];

        var output = Formatter.FormatProvisionalFinished(fixture);

        Assert.StartsWith("\nDefensive contributions:\n", output);
    }

    [Fact]
    public void NoBonusAndNoDefensiveContributions_ReturnsEmpty()
    {
        var output = Formatter.FormatProvisionalFinished(GetProvisionalFinishedFixture());
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void TopPerformersOnly_ListsPlayersByPointsDescending()
    {
        var fixture = GetProvisionalFinishedFixture();
        fixture.TopPerformers =
        [
            TopPerformer("player-B", 9),
            TopPerformer("player-A", 13),
            TopPerformer("player-C", 7)
        ];

        var output = Formatter.FormatProvisionalFinished(fixture);
        helper.WriteLine(output);

        Assert.StartsWith("\nTop performers:\n", output);
        Assert.DoesNotContain("Bonus points:", output);
        Assert.DoesNotContain("Defensive contributions:", output);
        Assert.Equal(["player-A (13p)", "player-B (9p)", "player-C (7p)"], Formatter.CreateTopPerformersOutput(fixture));
    }

    [Fact]
    public void TopPerformersWithSamePoints_AreOrderedByName()
    {
        var fixture = GetProvisionalFinishedFixture();
        fixture.TopPerformers =
        [
            TopPerformer("player-B", 9),
            TopPerformer("player-A", 9)
        ];

        Assert.Equal(["player-A (9p)", "player-B (9p)"], Formatter.CreateTopPerformersOutput(fixture));
    }

    [Fact]
    public void AllSections_TopPerformersComeLast()
    {
        var fixture = GetProvisionalFinishedFixture(
            BonusPointsPlayer("player-C", 30),
            BonusPointsPlayer("player-B", 40),
            BonusPointsPlayer("player-A", 50));
        fixture.DefensiveContributions = [DefensiveContributionPlayer("player-D", 11)];
        fixture.TopPerformers = [TopPerformer("player-A", 13)];

        var output = Formatter.FormatProvisionalFinished(fixture);
        helper.WriteLine(output);

        var bonusIndex = output.IndexOf("Bonus points:", StringComparison.Ordinal);
        var dcIndex = output.IndexOf("Defensive contributions:", StringComparison.Ordinal);
        var tpIndex = output.IndexOf("Top performers:", StringComparison.Ordinal);
        Assert.True(bonusIndex >= 0);
        Assert.True(dcIndex > bonusIndex);
        Assert.True(tpIndex > dcIndex);
        Assert.Contains("▪️ player-D (11)\n\nTop performers:\n▪️ player-A (13p)", output);
    }

    [Fact]
    public void BonusAndTopPerformers_WithoutDefensiveContributions_SeparatedByBlankLine()
    {
        var fixture = GetProvisionalFinishedFixture(
            BonusPointsPlayer("player-C", 30),
            BonusPointsPlayer("player-B", 40),
            BonusPointsPlayer("player-A", 50));
        fixture.TopPerformers = [TopPerformer("player-A", 13)];

        var output = Formatter.FormatProvisionalFinished(fixture);
        helper.WriteLine(output);

        Assert.DoesNotContain("Defensive contributions:", output);
        Assert.Contains("▪️ 1p player-C\n\nTop performers:\n▪️ player-A (13p)", output);
    }

    TopPerformer TopPerformer(string webName, int points)
    {
        return new TopPerformer
        {
            Player = new Player { WebName = webName },
            Points = points
        };
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

    DefensiveContributionPlayer DefensiveContributionPlayer(string webName, int contributions)
    {
        return new DefensiveContributionPlayer
        {
            Player = new Player { WebName = webName },
            Contributions = contributions
        };
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
