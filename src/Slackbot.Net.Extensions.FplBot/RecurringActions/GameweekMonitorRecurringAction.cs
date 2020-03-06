using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class GameweekMonitorRecurringAction : GameweekRecurringActionBase
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly ITeamsClient _teamsClient;
        private readonly IPlayerClient _playerClient;

        private ICollection<Fixture> _currentGameweekFixtures;

        public GameweekMonitorRecurringAction(
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ILogger<GameweekMonitorRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder,
            IFixtureClient fixtureClient,
            ITeamsClient teamsClient,
            IPlayerClient playerClient
            ) : base(options, gwClient, logger, tokenStore, slackClientBuilder)
        {
            _fixtureClient = fixtureClient;
            _teamsClient = teamsClient;
            _playerClient = playerClient;
        }

        protected override async Task DoStuffWhenInitialGameweekHasJustBegun(int newGameweek)
        {
            await Reset(newGameweek);
        }

        protected override async Task DoStuffWhenNewGameweekHaveJustBegun(int newGameweek)
        {
            await Reset(newGameweek);
        }

        private async Task Reset(int newGameweek)
        {
            _currentGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(newGameweek);
        }

        protected override async Task DoStuffWithinCurrentGameweek(int currentGameweek, bool isFinished)
        {
            if (isFinished)
            {
                return;
            }

            var newGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(currentGameweek);

            var players = await _playerClient.GetAllPlayers();
            var teams = await _teamsClient.GetAllTeams();

            var newFixtureEvents = newGameweekFixtures.Select(fixture =>
                {
                    var homeTeamName = teams.Single(t => t.Id == fixture.HomeTeamId).ShortName;
                    var awayTeamName = teams.Single(t => t.Id == fixture.AwayTeamId).ShortName;

                    Fixture oldFixture = _currentGameweekFixtures.FirstOrDefault(f => f.Code == fixture.Code);

                    var newFixtureStats = DiffFixtureStats(fixture, oldFixture, players);

                    return new FixtureEvents()
                    {
                        gameScore = new GameScore()
                        {
                            HomeTeamId = fixture.HomeTeamId,
                            AwayTeamId = fixture.AwayTeamId,
                            HomeTeamScore = fixture.HomeTeamScore,
                            AwayTeamScore = fixture.AwayTeamScore,
                            HomeTeamName = homeTeamName,
                            AwayTeamName = awayTeamName,
                        },
                        StatMap = newFixtureStats
                    };

                }).ToList();

            _currentGameweekFixtures = newGameweekFixtures;
        }

        private void PostNewEvents(List<FixtureEvents> newFixtureEvents)
        {
            newFixtureEvents.ForEach(newFixtureEvent =>
            {
                newFixtureEvent.StatMap.Keys.ToList().ForEach(statType =>
                {
                    switch (statType)
                    {
                        case StatType.GoalsScored:
                            // Format goals
                            break;
                        case StatType.Assists:
                            // Format assists
                            break;
                        case StatType.OwnGoals:
                            // Format own goals
                            break;
                        case StatType.RedCards:
                            // Format red cards
                            break;
                        case StatType.PenaltiesMissed:
                            // Format penalties missed
                            break;
                        case StatType.PenaltiesSaved:
                            // Format penalties saved
                            break;
                    }
                });
            });
        } 

        /*private async Task PostNewFixtureEvents(IDictionary<int, int> newGoalsByPlayer)
        {
            var players = await _playerClient.GetAllPlayers();
            foreach (var key in newGoalsByPlayer.Keys)
            {
                await Publish(async slackClient =>
                {
                    message = "";
                    return message;
                });
                await Task.Delay(2000);
            }
        }*/

        private static IDictionary<StatType, List<PlayerEvent>> DiffFixtureStats(Fixture newFixture, Fixture oldFixture, ICollection<Player> players)
        {
            var newFixtureStats = new Dictionary<StatType, List<PlayerEvent>>();

            foreach (FixtureStat stat in newFixture.Stats)
            {
                var type = StatTypeMethods.FromStatString(stat.Identifier);
                if (!type.HasValue)
                {
                    continue;
                }

                var diffs = new List<PlayerEvent>();

                FixtureStat oldStat = oldFixture.Stats.FirstOrDefault(s => s.Identifier == stat.Identifier);

                newFixtureStats.Add(type.Value, DiffStat(stat, oldStat, players));
            }

            return newFixtureStats;
        }

        private static List<PlayerEvent> DiffStat(FixtureStat newStat, FixtureStat oldStat, ICollection<Player> players)
        {
            var diffs = new List<PlayerEvent>();

            diffs.Concat(DiffStat(PlayerEvent.TeamType.Home, newStat.HomeStats, oldStat.HomeStats, players));
            diffs.Concat(DiffStat(PlayerEvent.TeamType.Away, newStat.AwayStats, oldStat.AwayStats, players));

            return diffs;
        }

        private static List<PlayerEvent> DiffStat(
            PlayerEvent.TeamType teamType,
            ICollection<FixtureStatValue> newStats,
            ICollection<FixtureStatValue> oldStats,
            ICollection<Player> players)
        {
            var diffs = new List<PlayerEvent>();

            foreach (FixtureStatValue newStat in newStats)
            {
                Player player = players.FirstOrDefault(p => p.Id == newStat.Element);
                string playerName = $"{player.FirstName} {player.SecondName}";

                var oldStat = oldStats.Where(old => old.Element == newStat.Element).FirstOrDefault();
                // Player had no stats from last check, so we add as new stat
                if (oldStat == null)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, playerName, teamType, false));
                    continue;
                }

                // Old player stat is same as new, so we skip it
                if (newStat.Value == oldStat.Value)
                {
                    continue;
                }
                // New stat for player is higher than old stat, so we add as new stat
                if (newStat.Value > oldStat.Value)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, playerName, teamType, false));
                    continue;
                }
                // New stat for player is lower than old stat, so we add as removed stat
                if (newStat.Value < oldStat.Value)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, playerName, teamType, true));
                    continue;
                }
            }

            foreach (FixtureStatValue oldStat in oldStats)
            {
                var newStat = newStats.Where(newStat => newStat.Element == oldStat.Element).FirstOrDefault();
                // Player had a stat previously that is now removed, so we add as removed stat
                if (newStat == null)
                {
                    Player player = players.FirstOrDefault(p => p.Id == newStat.Element);
                    string playerName = $"{player.FirstName} {player.SecondName}";
                    diffs.Add(new PlayerEvent(newStat.Element, playerName, teamType, true));
                }
            }

            return diffs;
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}
