using System.Collections.Generic;
using Fpl.Client.Models;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.Tests
{
    public static class TestBuilder
    {
        private const int HomeTeamId = 10;
        private const int AwayTeamId = 20;
        internal const int LeagueId = 111;
        
        internal const int PlayerId = 123;
        internal const int PlayerCode = 123;
        
        internal const int OtherPlayerId = 456;
        internal const int OtherPlayerCode = 456;
        
        internal const string SlackTeamId = "@T01337";

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
                TeamId = SlackTeamId,
                FplbotLeagueId = LeagueId,
                Subscriptions = new [] { EventSubscription.All }
            };
        }

        public static Player Player()
        {
            return new Player
            {
                Id = PlayerId,
                Code = PlayerCode,
                FirstName = "PlayerFirstName",
                SecondName = "PlayerSecondName"
            };
        }
        
        public static Player OtherPlayer()
        {
            return new Player
            {
                Id = OtherPlayerId,
                Code = OtherPlayerCode,
                FirstName = "OtherPlayerFirstName",
                SecondName = "OtherPlayerSecondName"
            };
        }
        
        public static Player WithCostChangeEvent(this Player player, int cost)
        {
            player.CostChangeEvent = cost;
            return player;
        }

        public static Player WithStatus(this Player player, string playerStatus)
        {
            player.Status = playerStatus;
            return player;
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