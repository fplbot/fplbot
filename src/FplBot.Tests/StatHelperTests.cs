using System.Linq;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Xunit;

namespace FplBot.Tests
{
    public class StatHelperTests
    {
        [Fact]
        public void NullFixtures_NoEvents()
        {
            var events = StatHelper.DiffFixtureStats(null, null);
            Assert.Empty(events);
        }
        
        [Fact]
        public void NullNewFixture_NoEvents()
        {
            var events = StatHelper.DiffFixtureStats(new Fixture(), null);
            Assert.Empty(events);
        }
        
        [Fact]
        public void NullOldFixture_NoEvents()
        {
            var events = StatHelper.DiffFixtureStats(null, new Fixture());
            Assert.Empty(events);
        }
        
        [Fact]
        public void SameFixture_NoEvents()
        {
            var fixture = new Fixture();
            var events = StatHelper.DiffFixtureStats(fixture,fixture);
            Assert.Empty(events);
        }
        
        [Fact]
        public void NewFixtureWithAdditionalStats_NewEvent()
        {
            var fixture = TestBuilder.NoGoals(fixtureCode:10);
            var fixtureWithGoal = TestBuilder.AwayTeamGoal(fixtureCode:10,goals:1);
            var events = StatHelper.DiffFixtureStats(newFixture:fixtureWithGoal,fixture);
            Assert.NotEmpty(events);
            Assert.Single(events);
            Assert.Equal(StatType.GoalsScored, events.First().Key);
            Assert.Equal(TestBuilder.PlayerId, events.First().Value.First().PlayerId);
        }
        
        [Fact]
        public void NewFixtureWithRemovedEvent_NewEvent_WithRemovedFlagTrue()
        {
            var fixtureWithGoal = TestBuilder.AwayTeamGoal(fixtureCode:10,goals:1);
            var fixtureGoalRemoved = TestBuilder.NoGoals(fixtureCode:10);
            var events = StatHelper.DiffFixtureStats(newFixture:fixtureGoalRemoved,fixtureWithGoal);
            Assert.NotEmpty(events);
            Assert.Single(events);
            Assert.Equal(StatType.GoalsScored, events.First().Key);
            var playerEvent = events.First().Value.First();
            Assert.Equal(TestBuilder.PlayerId, playerEvent.PlayerId);
            Assert.Equal(true, playerEvent.IsRemoved);
        }
    }
}