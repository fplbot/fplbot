using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using Xunit;

namespace FplBot.Tests
{
    public class StatHelperTests
    {
        [Fact]
        public void NullFixtures_NoEvents()
        {
            var events = FixtureDiffer.DiffFixtureStats(null, null, new List<Player>());
            Assert.Empty(events);
        }

        [Fact]
        public void NullNewFixture_NoEvents()
        {
            var events = FixtureDiffer.DiffFixtureStats(new Fixture(), null, new List<Player>());
            Assert.Empty(events);
        }

        [Fact]
        public void NullOldFixture_NoEvents()
        {
            var events = FixtureDiffer.DiffFixtureStats(null, new Fixture(), new List<Player>());
            Assert.Empty(events);
        }

        [Fact]
        public void SameFixture_NoEvents()
        {
            var fixture = new Fixture();
            var events = FixtureDiffer.DiffFixtureStats(fixture,fixture, new List<Player>());
            Assert.Empty(events);
        }

        [Fact]
        public void NewFixtureWithAdditionalStats_NewEvent()
        {
            var fixture = TestBuilder.NoGoals(fixtureCode:10);
            var fixtureWithGoal = TestBuilder.AwayTeamGoal(fixtureCode:10,goals:1);
            var events = FixtureDiffer.DiffFixtureStats(newFixture:fixtureWithGoal,fixture, new List<Player> { TestBuilder.Player()});
            Assert.NotEmpty(events);
            Assert.Single(events);
            Assert.Equal(StatType.GoalsScored, events.First().Key);
            Assert.Equal(TestBuilder.PlayerId, events.First().Value.First().Player.Id);
        }

        [Fact]
        public void NewFixtureWithRemovedEvent_NewEvent_WithRemovedFlagTrue()
        {
            var fixtureWithGoal = TestBuilder.AwayTeamGoal(fixtureCode:10,goals:1);
            var fixtureGoalRemoved = TestBuilder.NoGoals(fixtureCode:10);
            var events = FixtureDiffer.DiffFixtureStats(newFixture:fixtureGoalRemoved,fixtureWithGoal, new List<Player> { TestBuilder.Player()});
            Assert.NotEmpty(events);
            Assert.Single(events);
            Assert.Equal(StatType.GoalsScored, events.First().Key);
            var playerEvent = events.First().Value.First();
            Assert.Equal(TestBuilder.PlayerId, playerEvent.Player.Id);
            Assert.True(playerEvent.IsRemoved);
        }
    }
}
