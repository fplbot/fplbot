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

    [Fact]
    public void WithLiveItems_ResolvesTopPerformersOrderedByPoints()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();
        var liveItems = new[]
        {
            TestBuilder.LiveItem(TestBuilder.PlayerId, (fixture.Id, 8)),
            TestBuilder.LiveItem(TestBuilder.OtherPlayerId, (fixture.Id, 12))
        };

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, PlayersWithTeams(), fixture, liveItems);

        var topPerformers = finished.TopPerformers.ToList();
        Assert.Equal(2, topPerformers.Count);
        Assert.Equal(TestBuilder.OtherPlayerId, topPerformers[0].Player.Id);
        Assert.Equal(12, topPerformers[0].Points);
        Assert.Equal(TestBuilder.PlayerId, topPerformers[1].Player.Id);
        Assert.Equal(8, topPerformers[1].Points);
    }

    [Fact]
    public void DoubleGameweekPlayer_OnlyCountsPointsFromThisFixture()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();
        var liveItems = new[] { TestBuilder.LiveItem(TestBuilder.PlayerId, (fixture.Id, 6), (999, 10)) };

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, PlayersWithTeams(), fixture, liveItems);

        var only = Assert.Single(finished.TopPerformers);
        Assert.Equal(6, only.Points);
    }

    [Fact]
    public void PlayerWithoutExplainForThisFixture_IsExcluded()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();
        var liveItems = new[] { TestBuilder.LiveItem(TestBuilder.PlayerId, (999, 10)) };

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, PlayersWithTeams(), fixture, liveItems);

        Assert.Empty(finished.TopPerformers);
    }

    [Fact]
    public void UnknownPlayerInLiveItems_IsSkipped()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();
        var liveItems = new[]
        {
            TestBuilder.LiveItem(TestBuilder.PlayerId, (fixture.Id, 8)),
            TestBuilder.LiveItem(999999, (fixture.Id, 12))
        };

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, PlayersWithTeams(), fixture, liveItems);

        var only = Assert.Single(finished.TopPerformers);
        Assert.Equal(TestBuilder.PlayerId, only.Player.Id);
    }

    [Fact]
    public void PlayerFromAnotherTeam_IsSkipped()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();
        var players = Players(
            TestBuilder.Player().WithPosition(FplPlayerPosition.Defender).WithTeamId(TestBuilder.HomeTeam().Id),
            TestBuilder.OtherPlayer().WithPosition(FplPlayerPosition.Midfielder).WithTeamId(99));
        var liveItems = new[]
        {
            TestBuilder.LiveItem(TestBuilder.PlayerId, (fixture.Id, 8)),
            TestBuilder.LiveItem(TestBuilder.OtherPlayerId, (fixture.Id, 12))
        };

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, players, fixture, liveItems);

        var only = Assert.Single(finished.TopPerformers);
        Assert.Equal(TestBuilder.PlayerId, only.Player.Id);
    }

    [Fact]
    public void PlayersWithoutPoints_AreExcluded()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();
        var liveItems = new[]
        {
            TestBuilder.LiveItem(TestBuilder.PlayerId, (fixture.Id, 0)),
            TestBuilder.LiveItem(TestBuilder.OtherPlayerId, (fixture.Id, 2))
        };

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, PlayersWithTeams(), fixture, liveItems);

        var only = Assert.Single(finished.TopPerformers);
        Assert.Equal(TestBuilder.OtherPlayerId, only.Player.Id);
    }

    [Fact]
    public void MoreThanTheCap_KeepsTopPlayersAndThoseTiedWithTheLast()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();
        var points = new[] { 10, 9, 8, 7, 6, 6, 1 };
        var players = points.Select((_, i) => new Player { Id = i + 1, WebName = $"player-{i + 1}" }.WithTeamId(TestBuilder.HomeTeam().Id)).ToList();
        var liveItems = points.Select((p, i) => TestBuilder.LiveItem(i + 1, (fixture.Id, p))).ToList();

        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, players, fixture, liveItems);

        var topPerformers = finished.TopPerformers.ToList();
        Assert.Equal(FixtureFulltimeModelBuilder.TopPerformersCount + 1, topPerformers.Count);
        Assert.Equal(new[] { 10, 9, 8, 7, 6, 6 }, topPerformers.Select(tp => tp.Points).ToArray());
        Assert.DoesNotContain(topPerformers, tp => tp.Points == 1);
    }

    [Fact]
    public void WithoutLiveItems_ReturnsEmptyTopPerformers()
    {
        var fixture = TestBuilder.AwayTeamGoal(1, 1).FinishedProvisional();

        var withNull = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, PlayersWithTeams(), fixture);
        var withEmpty = FixtureFulltimeModelBuilder.CreateFinishedFixture(Teams, PlayersWithTeams(), fixture, new List<LiveItem>());

        Assert.Empty(withNull.TopPerformers);
        Assert.Empty(withEmpty.TopPerformers);
    }

    // TestBuilder players only carry a TeamCode. Top performers are matched on TeamId, so set it here.
    private static ICollection<Player> PlayersWithTeams() => Players(
        TestBuilder.Player().WithPosition(FplPlayerPosition.Defender).WithTeamId(TestBuilder.HomeTeam().Id),
        TestBuilder.OtherPlayer().WithPosition(FplPlayerPosition.Midfielder).WithTeamId(TestBuilder.AwayTeam().Id));

    private static ICollection<Player> DefaultPlayers() => Players(
        TestBuilder.Player().WithPosition(FplPlayerPosition.Defender),
        TestBuilder.OtherPlayer().WithPosition(FplPlayerPosition.Midfielder));

    private static ICollection<Player> Players(params Player[] players) => players;
}
