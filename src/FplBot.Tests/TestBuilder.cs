using System.Collections.Generic;
using Fpl.Client.Models;

namespace FplBot.Tests
{
    public class TestBuilder
    {
        public static Fixture NoGoals(int fixtureCode)
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
        public static Fixture AwayTeamGoal(int fixtureCode, int goals)
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
    }
}