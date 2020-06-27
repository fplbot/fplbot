using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;

namespace FplBot.Tests
{
    public class TestBuilder
    {
        private const int HomeTeamId = 10;
        private const int AwayTeamId = 20;

        public static Fixture NoGoals(int fixtureCode)
        {
            return new Fixture
            {
                Code = fixtureCode,
                HomeTeamId = 10,
                AwayTeamId = AwayTeamId,
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
        public static Fixture AwayTeamGoal(int fixtureCode, int goals)
        {
            return new Fixture
            {
                Code = fixtureCode,
                HomeTeamId = HomeTeamId,
                AwayTeamId = AwayTeamId,
                Stats = new[]
                {
                    AwayTeamLeadingBy(goals)
                }
            };
        }

        private static FixtureStat AwayTeamLeadingBy(int goals)
        {
            return new FixtureStat
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
            };
        }

        public static Team AwayTeam()
        {
            return new Team
            {
                Id = AwayTeamId
            };
        }
    }
}