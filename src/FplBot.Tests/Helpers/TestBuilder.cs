using Fpl.Client.Models;
using Fpl.EventPublishers;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests;

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
            Started = true,
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
                },
                new FixtureStat
                {
                    Identifier = "own_goals",
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

    public static Fixture AwayTeamGoal(int fixtureCode, int goals, int? minutes = null)
    {
        return new Fixture
        {
            Id = fixtureCode,
            Code = fixtureCode,
            HomeTeamId = HomeTeamId,
            AwayTeamId = AwayTeamId,
            Started = true,
            Stats = new[]
            {
                AddGoalsScored(goals, PlayerId)
            },
            HomeTeamScore = 0,
            AwayTeamScore = goals,
            PulseId = fixtureCode,
            Minutes = minutes.HasValue ? minutes.Value : 30
        };
    }

    public static Fixture AddAwayGoal(this Fixture fixture, int numGoals = 1)
    {
        var goalScoredStats = fixture.Stats.FirstOrDefault(c => c.Identifier == "goals_scored");

        var awayGoal = new FixtureStatValue
        {
            Element = PlayerId,
            Value = numGoals
        };

        if (goalScoredStats is null)
        {
            goalScoredStats = new FixtureStat
            {
                Identifier = "goals_scored",
                HomeStats = Array.Empty<FixtureStatValue>(),
                AwayStats = new[] { awayGoal }
            };
            var updatedStats = fixture.Stats.Append(goalScoredStats);
            fixture.Stats = updatedStats.ToArray();
        }
        else
        {
            var updatedAwayStats = goalScoredStats.AwayStats.Append(awayGoal);
            goalScoredStats.AwayStats = updatedAwayStats.ToArray();
        }

        return fixture;
    }

    public static Fixture AddHomeGoal(this Fixture fixture, int numGoals = 1)
    {
        var goalScoredStats = fixture.Stats.FirstOrDefault(c => c.Identifier == "goals_scored");

        var homeGoal = new FixtureStatValue
        {
            Element = PlayerId,
            Value = numGoals
        };

        if (goalScoredStats is null)
        {
            goalScoredStats = new FixtureStat
            {
                Identifier = "goals_scored",
                HomeStats = new[] { homeGoal },
                AwayStats = Array.Empty<FixtureStatValue>(),
            };
            var updatedStats = fixture.Stats.Append(goalScoredStats);
            fixture.Stats = updatedStats.ToArray();
        }
        else
        {
            var updatedAwayStats = goalScoredStats.HomeStats.Append(homeGoal);
            goalScoredStats.HomeStats = updatedAwayStats.ToArray();
        }

        return fixture;
    }

    public static Fixture AddAwayOwnGoal(this Fixture fixture, int numGoals = 1)
    {
        var ownGoalsStats = fixture.Stats.FirstOrDefault(c => c.Identifier == "own_goals");

        var awayOwnGoal = new FixtureStatValue
        {
            Element = PlayerId,
            Value = numGoals
        };

        if (ownGoalsStats is null)
        {
            ownGoalsStats = new FixtureStat
            {
                Identifier = "own_goals",
                HomeStats = Array.Empty<FixtureStatValue>(),
                AwayStats = new[] { awayOwnGoal }
            };
            var updatedStats = fixture.Stats.Append(ownGoalsStats);
            fixture.Stats = updatedStats.ToArray();
        }
        else
        {
            var updatedAwayStats = ownGoalsStats.AwayStats.Append(awayOwnGoal);
            ownGoalsStats.AwayStats = updatedAwayStats.ToArray();
        }

        return fixture;
    }

    public static Fixture FinishedProvisional(this Fixture fixture)
    {
        fixture.FinishedProvisional = true;
        return fixture;
    }

    public static Fixture NotStarted(this Fixture fixture)
    {
        fixture.Started = false;
        return fixture;
    }

    public static Fixture WithProvisionalBonus(this Fixture fixture, int playerId, int bpsValue)
    {
        fixture.FinishedProvisional = true;
        fixture.Stats = fixture.Stats.Append(BpsSystem(playerId,bpsValue)).ToArray();
        return fixture;
    }

    public static Fixture WithYellowCard(this Fixture fixture)
    {
        fixture.Stats = fixture.Stats.Append(Yellow(PlayerId)).ToArray();
        return fixture;
    }

    public static Fixture WithSaves(this Fixture fixture)
    {
        fixture.Stats = fixture.Stats.Append(Saves(PlayerId)).ToArray();
        return fixture;
    }

    public static Fixture WithBonus(this Fixture fixture)
    {
        fixture.Stats = fixture.Stats.Append(Bonus(PlayerId)).ToArray();
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

    private static FixtureStat Yellow(int playerId)
    {
        return CreateStat(playerId, "yellow_cards");
    }

    private static FixtureStat Bonus(int playerId)
    {
        return CreateStat(playerId, "bonus");
    }

    private static FixtureStat Saves(int playerId)
    {
        return CreateStat(playerId, "saves");
    }

    private static FixtureStat CreateStat(int playerId, string identifier)
    {
        return new FixtureStat
        {
            Identifier = identifier,
            HomeStats = new List<FixtureStatValue>
            {
                new FixtureStatValue { Element = playerId, Value = 1 }
            },
            AwayStats = new List<FixtureStatValue> { }
        };
    }

    private static FixtureStat AddGoalsScored(int goals, int playerId)
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
                    Element = playerId,
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
            Code = HomeTeamId,
            Name = "HomeTeam",
            ShortName = "HOM"
        };
    }

    public static Team AwayTeam()
    {
        return new Team
        {
            Id = AwayTeamId,
            Code = AwayTeamId,
            Name = "AwAyTeam",
            ShortName = "AWA"
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
            SecondName = "PlayerSecondName",
            WebName = "PlayerWebname",
            TeamCode = HomeTeamId
        };
    }

    public static Player OtherPlayer()
    {
        return new Player
        {
            Id = OtherPlayerId,
            Code = OtherPlayerCode,
            FirstName = "OtherPlayerFirstName",
            SecondName = "OtherPlayerSecondName",
            TeamCode = AwayTeamId
        };
    }

    public static Player FromHomeTeam(this Player player)
    {
        player.TeamCode = HomeTeamId;
        return player;
    }

    public static Player FromAwayTeam(this Player player)
    {
        player.TeamCode = AwayTeamId;
        return player;
    }

    public static Player WithCost(this Player player, int cost)
    {
        player.NowCost = cost;
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
                    Formation = new Formation { Players = new List<int[]>{ new []{ 1 } } },
                    Lineup = new []{ new PlayerInLineup { Id = 1, Name = new Name { First = "First", Last = "Lasteson", Display = "Lastinho"}, MatchPosition = "D"} }
                },
                new LineupContainer
                {
                    Formation = new Formation
                    {
                        Players = new List<int[]>{ new []{ 1 } }

                    },
                    Lineup = new []{ new PlayerInLineup { Id = 1, Name = new Name { First = "First", Last = "Lasteson", Display = "Lastinho"}, MatchPosition = "D"} }
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

    public static PlayerDetails PlayerDetails()
    {
        var player = Player();
        return new PlayerDetails(player.Id, player.WebName);
    }
}
