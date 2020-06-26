using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.RecurringActions;
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
            ICollection<Fixture> current = new List<Fixture>
            {
                NoGoals(fixtureCode:1)
            };
            
            ICollection<Fixture> latest = new List<Fixture>
            {
                AwayTeamGoal(fixtureCode:1, goals: 1)
            };
          
            var events = GameweekMonitorRecurringAction.GetUpdatedFixtureEvents(latest, current);
            Assert.NotEmpty(events);
            Assert.Equal(1337, events.First().StatMap[StatType.GoalsScored].First().PlayerId);
            Assert.Equal(PlayerEvent.TeamType.Away, events.First().StatMap[StatType.GoalsScored].First().Team);
            
        }

        private static Fixture NoGoals(int fixtureCode)
        {
            return new Fixture
            {
                Code = fixtureCode,
                HomeTeamId = 10,
                AwayTeamId = 20,
                Stats = new[]
                {
                    new FixtureStat
                    {
                        Identifier = "goals_scored",
                        HomeStats = new List<FixtureStatValue>
                        {
                            
                        },
                        AwayStats = new List<FixtureStatValue>
                        {
                        }
                            
                    }
                }
            };
        }
        private static Fixture AwayTeamGoal(int fixtureCode, int goals)
        {
            return new Fixture
            {
                Code = fixtureCode,
                HomeTeamId = 10,
                AwayTeamId = 20,
                Stats = new[]
                {
                    new FixtureStat
                    {
                        Identifier = "goals_scored",
                        HomeStats = new List<FixtureStatValue>
                        {
                            
                        },
                        AwayStats = new List<FixtureStatValue>
                        {
                            new FixtureStatValue
                            {
                                Element = 1337,
                                Value = goals
                            }
                        }
                            
                    }
                }
            };
        }

        private static void AssertEmpty(ICollection<Fixture> latest, ICollection<Fixture> current)
        {
            var events = GameweekMonitorRecurringAction.GetUpdatedFixtureEvents(latest, current);
            Assert.Empty(events);
        }
    }

}