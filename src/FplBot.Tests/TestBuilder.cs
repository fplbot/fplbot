using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using Slackbot.Net.Endpoints.Models;

namespace FplBot.Tests
{
    public class TestBuilder
    {
        private const int HomeTeamId = 10;
        private const int AwayTeamId = 20;
        internal const int LeagueId = 111;
        internal const int PlayerId = 123;

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
                        Element = PlayerId,
                        Value = goals
                    }
                }
            };
        }
        
        public static Team HomeTeam()
        {
            return new Team
            {
                Id = HomeTeamId
            };
        }

        public static Team AwayTeam()
        {
            return new Team
            {
                Id = AwayTeamId
            };
        }

        public static SlackTeam SlackTeam()
        {
            return new SlackTeam
            {
                FplbotLeagueId = LeagueId
            };
        }

        public static Player Player()
        {
            return new Player
            {
                Id = PlayerId,
                FirstName = "PlayerFirstName",
                SecondName = "PlayerSecondName"
            };
        }
        
        public static Gameweek OlderGameweek(int id)
        {
            return new Gameweek
            {
                Id = id
            };
        }

        public static Gameweek CurrentGameweek(int id)
        {
            return new Gameweek
            {
                Id = id,
                IsCurrent = true
            };
        }
        
        public static Gameweek PreviousGameweek(int id)
        {
            return new Gameweek
            {
                Id = id,
                IsPrevious = true
            };
        }
        
        public static Gameweek NextGameweek(int id)
        {
            return new Gameweek
            {
                Id = id,
                IsNext = true
            };
        }
    }
}