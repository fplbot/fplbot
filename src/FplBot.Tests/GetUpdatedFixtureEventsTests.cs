using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests;

public class GetUpdatedFixtureEventsTests
{
    [Fact]
    public static void When_NoEntries_ReturnsEmptyList()
    {
        AssertEmpty(null,null);
        AssertEmpty(new List<Fixture>(), null);
        AssertEmpty(null, new List<Fixture>());

        void AssertEmpty(ICollection<Fixture> latest, ICollection<Fixture> current)
        {
            var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current, new List<Player>(), new List<Team>());
            Assert.Empty(events);
        }
    }

    [Fact]
    public static void When_NewAwayGoal_ReturnsAwayTeamGoalEvent()
    {
        var current = new List<Fixture>
        {
            TestBuilder.NoGoals(fixtureCode:1)
        };

        var latest = new List<Fixture>
        {
            TestBuilder.AwayTeamGoal(fixtureCode:1, goals: 1, minutes: 72)
        };

        var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current, new List<Player> { TestBuilder.Player()}, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});
        var awayGoalEvent = events.First();
        Assert.Equal(123, awayGoalEvent.StatMap[StatType.GoalsScored].First().Player.Id);
        Assert.Equal(TeamType.Away, awayGoalEvent.StatMap[StatType.GoalsScored].First().Team);
        Assert.Equal(72, awayGoalEvent.FixtureScore.Minutes);
        Assert.Equal(0, awayGoalEvent.FixtureScore.HomeTeamScore);
        Assert.Equal(1, awayGoalEvent.FixtureScore.AwayTeamScore);
    }

    [Fact]
    public static void When_DifferentScorers_ReturnsCorrectScore()
    {
        var current = new List<Fixture>
        {
            TestBuilder.NoGoals(fixtureCode:1)
        };

        var updatedFixture = TestBuilder.NoGoals(1);
        updatedFixture.AddAwayGoal();
        updatedFixture.AddAwayGoal();
        updatedFixture.AddAwayGoal();

        var latest = new List<Fixture>
        {
            updatedFixture
        };

        var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current, new List<Player> { TestBuilder.Player()}, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});
        var awayGoalEvent = events.First();
        Assert.Equal(0, awayGoalEvent.FixtureScore.HomeTeamScore);
        Assert.Equal(3, awayGoalEvent.FixtureScore.AwayTeamScore);
    }

    [Fact]
    public static void When_NoStats_ReturnsEmpty()
    {
        var current = new List<Fixture>
        {
            TestBuilder.NoGoals(fixtureCode:1)
        };

        var latest = new List<Fixture>
        {
            TestBuilder.NoGoals(fixtureCode:1)
        };

        var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current, new List<Player> { TestBuilder.Player()}, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});
        Assert.Empty(events);
    }

    [Fact]
    public static void When_OwnGoal_ReturnsCorrectScore()
    {
        var current = new List<Fixture>
        {
            TestBuilder.NoGoals(fixtureCode:1)
        };

        var updatedFixture = TestBuilder.NoGoals(1);
        updatedFixture.AddAwayOwnGoal();

        var latest = new List<Fixture>
        {
            updatedFixture
        };

        var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current, new List<Player> { TestBuilder.Player()}, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Equal(1, events.First().FixtureScore.HomeTeamScore);
        Assert.Equal(0, events.First().FixtureScore.AwayTeamScore);
    }

    [Fact]
    public static void When_OwnGoalAndGoal_ReturnsCorrectScore()
    {
        var current = new List<Fixture>
        {
            TestBuilder.NoGoals(fixtureCode:1)
        };

        var updatedFixture = TestBuilder.NoGoals(1);
        updatedFixture.AddHomeGoal();
        updatedFixture.AddAwayOwnGoal();

        var latest = new List<Fixture>
        {
            updatedFixture
        };

        var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current, new List<Player> { TestBuilder.Player()}, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Equal(2, events.First().FixtureScore.HomeTeamScore);
        Assert.Equal(0, events.First().FixtureScore.AwayTeamScore);
    }
}
