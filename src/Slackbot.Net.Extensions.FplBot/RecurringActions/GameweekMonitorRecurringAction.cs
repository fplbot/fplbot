using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class GameweekMonitorRecurringAction : GameweekRecurringActionBase
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly ITransfersByGameWeek _transfersByGameWeek;
        private IEnumerable<TransfersByGameWeek.Transfer> _transfersForCurrentGameweek;
        private readonly GameweekEventsFormatter _gameweekEventsFormatter;

        private ICollection<Fixture> _currentGameweekFixtures;

        public GameweekMonitorRecurringAction(
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ILogger<GameweekMonitorRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder,
            ITransfersByGameWeek transfersByGameWeek,
            IFixtureClient fixtureClient,
            GameweekEventsFormatter gameweekEventsFormatter
            ) : base(options, gwClient, logger, tokenStore, slackClientBuilder)
        {
            _fixtureClient = fixtureClient;
            _gameweekEventsFormatter = gameweekEventsFormatter;
            _transfersByGameWeek = transfersByGameWeek;
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
            _transfersForCurrentGameweek = await _transfersByGameWeek.GetTransfersByGameweek(newGameweek);
        }

        protected override async Task DoStuffWithinCurrentGameweek(int currentGameweek, bool isFinished)
        {
            if (isFinished)
            {
                return;
            }

            var newGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(currentGameweek);

            var newFixtureEvents = newGameweekFixtures.Select(fixture =>
                {
                    Fixture oldFixture = _currentGameweekFixtures.FirstOrDefault(f => f.Code == fixture.Code);

                    var newFixtureStats = DiffFixtureStats(fixture, oldFixture);

                    return new FixtureEvents()
                    {
                        gameScore = new GameScore()
                        {
                            HomeTeamId = fixture.HomeTeamId,
                            AwayTeamId = fixture.AwayTeamId,
                            HomeTeamScore = fixture.HomeTeamScore,
                            AwayTeamScore = fixture.AwayTeamScore,
                        },
                        StatMap = newFixtureStats
                    };

                }).ToList();

            var formattedEvents = await _gameweekEventsFormatter.FormatNewFixtureEvents(newFixtureEvents, _transfersForCurrentGameweek);
            await PostNewEvents(formattedEvents);

            _currentGameweekFixtures = newGameweekFixtures;
        }

        private async Task PostNewEvents(List<string> events)
        {
            foreach (string s in events)
            {
                await Publish(async slackClient => await Task.FromResult(s));
                await Task.Delay(2000);
            }
        }

        private static IDictionary<StatType, List<PlayerEvent>> DiffFixtureStats(Fixture newFixture, Fixture oldFixture)
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

                newFixtureStats.Add(type.Value, DiffStat(stat, oldStat));
            }

            return newFixtureStats;
        }

        private static List<PlayerEvent> DiffStat(FixtureStat newStat, FixtureStat oldStat)
        {
            var diffs = new List<PlayerEvent>();

            diffs.Concat(DiffStat(PlayerEvent.TeamType.Home, newStat.HomeStats, oldStat.HomeStats));
            diffs.Concat(DiffStat(PlayerEvent.TeamType.Away, newStat.AwayStats, oldStat.AwayStats));

            return diffs;
        }

        private static List<PlayerEvent> DiffStat(
            PlayerEvent.TeamType teamType,
            ICollection<FixtureStatValue> newStats,
            ICollection<FixtureStatValue> oldStats)
        {
            var diffs = new List<PlayerEvent>();

            foreach (FixtureStatValue newStat in newStats)
            {
                var oldStat = oldStats.Where(old => old.Element == newStat.Element).FirstOrDefault();
                // Player had no stats from last check, so we add as new stat
                if (oldStat == null)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, teamType, false));
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
                    diffs.Add(new PlayerEvent(newStat.Element, teamType, false));
                    continue;
                }
                // New stat for player is lower than old stat, so we add as removed stat
                if (newStat.Value < oldStat.Value)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, teamType, true));
                    continue;
                }
            }

            foreach (FixtureStatValue oldStat in oldStats)
            {
                var newStat = newStats.Where(newStat => newStat.Element == oldStat.Element).FirstOrDefault();
                // Player had a stat previously that is now removed, so we add as removed stat
                if (newStat == null)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, teamType, true));
                }
            }

            return diffs;
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}
