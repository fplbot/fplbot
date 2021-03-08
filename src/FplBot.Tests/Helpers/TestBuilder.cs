using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Models;
using Fpl.Data;
using Fpl.Data.Models;
using Fpl.Data.Repositories;
using FplBot.Core;
using FplBot.Core.Abstractions;
using Slackbot.Net.Endpoints.Models;

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
                Id = fixtureCode,
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
                }, 
                PulseId = fixtureCode
            };
        }
        public static Fixture AwayTeamGoal(int fixtureCode, int goals)
        {
            return new Fixture
            {
                Id = fixtureCode,
                Code = fixtureCode,
                HomeTeamId = HomeTeamId,
                AwayTeamId = AwayTeamId,
                Stats = new[]
                {
                    AwayTeamLeadingBy(goals)
                },
                HomeTeamScore = 0,
                AwayTeamScore = goals,
                PulseId = fixtureCode
            };
        }

        public static Fixture FinishedProvisional(this Fixture fixture)
        {
            fixture.FinishedProvisional = true;
            return fixture;
        }

        public static Fixture WithProvisionalBonus(this Fixture fixture, int playerId, int bpsValue)
        {
            fixture.FinishedProvisional = true;
            fixture.Stats = fixture.Stats.Append(BpsSystem(playerId,bpsValue)).ToArray();
            return fixture;
        }

        private static FixtureStat BpsSystem(int playerId, int bps)
        {
            return new FixtureStat
            {
                Identifier = "bps",
                HomeStats = new List<FixtureStatValue>
                {
                    new FixtureStatValue {Element = playerId, Value = bps}
                },
                AwayStats = new List<FixtureStatValue> { }
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
                Id = HomeTeamId,
                ShortName = "HoMeTeam"
            };
        }

        public static Team AwayTeam()
        {
            return new Team
            {
                Id = AwayTeamId,
                ShortName = "AwAyTeam"
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
        
        public static Player WithNews(this Player player, string news)
        {
            player.News = news;
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

        public static MatchDetails NoLineup(int pulseFixtureId)
        {
            return new MatchDetails
            {
                Id = pulseFixtureId,
                Teams = SomeHomeAndAwayTeams(),
                TeamLists = null
            };
        }

        public static MatchDetails Lineup(int pulseFixtureId)
        {
            return new MatchDetails
            {
                Id = pulseFixtureId,
                Teams = SomeHomeAndAwayTeams(),
                
                TeamLists = new []
                {
                    new LineupContainer
                    {
                        Formation = new Formation { Players = new IEnumerable<int>[0]},
                        Lineup = new []{ new PlayerInLineup() }
                    }, 
                    new LineupContainer
                    {
                        Formation = new Formation { Players = new IEnumerable<int>[0]},
                        Lineup = new []{ new PlayerInLineup() }
                    }
                }
            };
        }

        private static List<TeamDetails> SomeHomeAndAwayTeams()
        {
            return new List<TeamDetails>
            {
                new TeamDetails { Team = new PulseTeam { Club = new Club()}},
                new TeamDetails { Team = new PulseTeam { Club = new Club()}}
            };
        }
    }
}