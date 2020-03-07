using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.SlackClients.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Extensions;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class GameweekMonitorRecurringAction : GameweekRecurringActionBase
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly ITransfersByGameWeek _transfersByGameWeek;
        private IEnumerable<TransfersByGameWeek.Transfer> _transfersForCurrentGameweek;
        private readonly IPlayerClient _playerClient;
        private ICollection<Team> _teams;
        private readonly ITeamsClient _teamsClient;
        private ICollection<Player> _players;

        private ICollection<Fixture> _currentGameweekFixtures;

        public GameweekMonitorRecurringAction(IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ILogger<GameweekMonitorRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder,
            ITransfersByGameWeek transfersByGameWeek,
            IFixtureClient fixtureClient,
            IPlayerClient playerClient,
            ITeamsClient teamsClient, 
            IFetchFplbotSetup teamRepo) : base(options, gwClient, logger, tokenStore, slackClientBuilder, teamRepo)
        {
            _fixtureClient = fixtureClient;
            _transfersByGameWeek = transfersByGameWeek;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
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
            _transfersForCurrentGameweek = await _transfersByGameWeek.GetTransfersByGameweek(newGameweek, _options.Value.LeagueId);
            _players = await _playerClient.GetAllPlayers();
            _teams = await _teamsClient.GetAllTeams();
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
                    var oldFixture = _currentGameweekFixtures.FirstOrDefault(f => f.Code == fixture.Code);

                    var newFixtureStats = StatHelper.DiffFixtureStats(fixture, oldFixture);

                    return newFixtureStats.Values.Any() ? new FixtureEvents
                    {
                        GameScore = new GameScore
                        {
                            HomeTeamId = fixture.HomeTeamId,
                            AwayTeamId = fixture.AwayTeamId,
                            HomeTeamScore = fixture.HomeTeamScore,
                            AwayTeamScore = fixture.AwayTeamScore,
                        },
                        StatMap = newFixtureStats
                    } : null;

                }).WhereNotNull().ToList();

            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newFixtureEvents, _transfersForCurrentGameweek, _players, _teams);
            await PostNewEvents(formattedEvents);

            _currentGameweekFixtures = newGameweekFixtures;
        }

        private async Task PostNewEvents(List<string> events)
        {
            foreach (var s in events)
            {
                await Publish(async slackClient => await Task.FromResult(s));
                await Task.Delay(2000);
            }
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}
