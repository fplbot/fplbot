using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using Xunit;

namespace FplBot.Tests
{
    public class GetUpdatedFixtureEventsTests
    {
        [Fact]
        public static void When_NoEntries_ReturnsEmptyList()
        {
            AssertEmpty(null,null);
            AssertEmpty(new List<Fixture>(), null);
            AssertEmpty(null, new List<Fixture>());
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
                TestBuilder.AwayTeamGoal(fixtureCode:1, goals: 1)
            };
          
            var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current);
            var awayGoalEvent = events.First();
            Assert.Equal(123, awayGoalEvent.StatMap[StatType.GoalsScored].First().PlayerId);
            Assert.Equal(PlayerEvent.TeamType.Away, awayGoalEvent.StatMap[StatType.GoalsScored].First().Team);
        }

        private static void AssertEmpty(ICollection<Fixture> latest, ICollection<Fixture> current)
        {
            var events = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, current);
            Assert.Empty(events);
        }
    }
}