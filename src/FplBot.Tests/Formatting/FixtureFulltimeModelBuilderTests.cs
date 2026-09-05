using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;

namespace FplBot.Tests.Formatting;

public class FixtureFulltimeModelBuilderTests
{
    private static readonly ICollection<Team> Teams = new[] { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() };

    [Fact]
    public void WithDefensiveContributionStat_ResolvesQualifyingPlayersOrderedByContributions()
    {
        var players = Players(
            TestBuilder.Player().WithPosition(FplPlayerPosition.Defender),
            TestBuilder.OtherPlayer().WithPosition(FplPlayerPosition.Midfielder));
        var fixture = TestBuilder.AwayTeamGoal(1, 1)
            .FinishedProvisional()
            .WithDefensiveContribution(TestBuilder.PlayerId, 10, TestBuilder.OtherPlayerId, 13);

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, players, fixture);

        var contributions = finished.DefensiveContributions.ToList();
        Assert.Equal(2, contributions.Count);
        Assert.Equal(TestBuilder.OtherPlayerId, contributions[0].Player.Id);
        Assert.Equal(13, contributions[0].Contributions);
        Assert.Equal(TestBuilder.PlayerId, contributions[1].Player.Id);
        Assert.Equal(10, contributions[1].Contributions);
    }

    [Fact]
    public void PlayersBelowThreshold_AreExcluded()
    {
        var players = Players(
            TestBuilder.Player().WithPosition(FplPlayerPosition.Defender),
            TestBuilder.OtherPlayer().WithPosition(FplPlayerPosition.Midfielder));
        var fixture = TestBuilder.AwayTeamGoal(1, 1)
            .FinishedProvisional()
            .WithDefensiveContribution(TestBuilder.PlayerId, 9, TestBuilder.OtherPlayerId, 11);

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, players, fixture);

        Assert.Empty(finished.DefensiveContributions);
    }

    [Fact]
    public void Goalkeeper_IsExcludedRegardlessOfCount()
    {
        var players = Players(
            TestBuilder.Player().WithPosition(FplPlayerPosition.Goalkeeper),
            TestBuilder.OtherPlayer().WithPosition(FplPlayerPosition.Forward));
        var fixture = TestBuilder.AwayTeamGoal(1, 1)
            .FinishedProvisional()
            .WithDefensiveContribution(TestBuilder.PlayerId, 20, TestBuilder.OtherPlayerId, 12);

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, players, fixture);

        var only = Assert.Single(finished.DefensiveContributions);
        Assert.Equal(TestBuilder.OtherPlayerId, only.Player.Id);
    }

    [Theory]
    [InlineData(FplPlayerPosition.Goalkeeper, 30, false)]
    [InlineData(FplPlayerPosition.Defender, 9, false)]
    [InlineData(FplPlayerPosition.Defender, 10, true)]
    [InlineData(FplPlayerPosition.Midfielder, 11, false)]
    [InlineData(FplPlayerPosition.Midfielder, 12, true)]
    [InlineData(FplPlayerPosition.Forward, 11, false)]
    [InlineData(FplPlayerPosition.Forward, 12, true)]
    [InlineData(FplPlayerPosition.NotSet, 30, false)]
    public void ReachedThreshold_FollowsPositionRules(FplPlayerPosition position, int contributions, bool expected)
    {
        var dc = new DefensiveContributionPlayer
        {
            Player = new Player { Position = position },
            Contributions = contributions
        };

        Assert.Equal(expected, FixtureFulltimeModelBuilder.ReachedThreshold(dc));
    }

    [Fact]
    public void WithoutDefensiveContributionStat_ReturnsEmpty()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, DefaultPlayers(), fixture);

        Assert.Empty(finished.DefensiveContributions);
    }

    [Fact]
    public void WithEmptyDefensiveContributionStat_ReturnsEmpty()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional().WithEmptyDefensiveContribution();

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, DefaultPlayers(), fixture);

        Assert.Empty(finished.DefensiveContributions);
    }

    [Fact]
    public void WithUnknownPlayerInDefensiveContributionStat_SkipsThatPlayer()
    {
        var players = Players(TestBuilder.Player().WithPosition(FplPlayerPosition.Defender));
        var fixture = TestBuilder.AwayTeamGoal(1, 1)
            .FinishedProvisional()
            .WithDefensiveContribution(TestBuilder.PlayerId, 11, 999999, 12);

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, players, fixture);

        var only = Assert.Single(finished.DefensiveContributions);
        Assert.Equal(TestBuilder.PlayerId, only.Player.Id);
    }

    [Fact]
    public void BonusPointsStillBuiltFromBps()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).WithProvisionalBonus(TestBuilder.PlayerId, 30);

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, DefaultPlayers(), fixture);

        var only = Assert.Single(finished.BonusPoints);
        Assert.Equal(30, only.BonusPoints);
    }

    private static ICollection<Player> DefaultPlayers() => Players(
        TestBuilder.Player().WithPosition(FplPlayerPosition.Defender),
        TestBuilder.OtherPlayer().WithPosition(FplPlayerPosition.Midfielder));

    private static ICollection<Player> Players(params Player[] players) => players;
}
