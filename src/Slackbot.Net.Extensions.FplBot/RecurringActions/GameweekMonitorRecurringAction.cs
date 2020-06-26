﻿using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
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
        private IDictionary<int, IEnumerable<TransfersByGameWeek.Transfer>> _transfersForCurrentGameweek;
        private readonly IPlayerClient _playerClient;
        private ICollection<Team> _teams;
        private readonly ITeamsClient _teamsClient;
        private ICollection<Player> _players;

        private ICollection<Fixture> _currentGameweekFixtures;

        public GameweekMonitorRecurringAction(
            IGameweekClient gwClient,
            ILogger<GameweekMonitorRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder,
            ITransfersByGameWeek transfersByGameWeek,
            IFixtureClient fixtureClient,
            IPlayerClient playerClient,
            ITeamsClient teamsClient,
            IFetchFplbotSetup teamRepo) : base(gwClient, logger, tokenStore, slackClientBuilder, teamRepo)
        {
            _fixtureClient = fixtureClient;
            _transfersByGameWeek = transfersByGameWeek;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _transfersForCurrentGameweek = new Dictionary<int, IEnumerable<TransfersByGameWeek.Transfer>>();
            _currentGameweekFixtures = new List<Fixture>();
            _players = new List<Player>();
            _teams = new List<Team>();
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
            _players = await _playerClient.GetAllPlayers();
            _teams = await _teamsClient.GetAllTeams();
            
            _transfersForCurrentGameweek.Clear();
            
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var c = await _teamRepo.GetSetupByToken(token);
                var transferForLeague = await _transfersByGameWeek.GetTransfersByGameweek(newGameweek, c.LeagueId);
                _transfersForCurrentGameweek.Add(c.LeagueId, transferForLeague);
            }
        }

        protected override async Task DoStuffWithinCurrentGameweek(int currentGameweek, bool isFinished)
        {
            if (isFinished)
            {
                return;
            }
            
            // Check for new Slack workspaces installed during a gameweek being monitored
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var c = await _teamRepo.GetSetupByToken(token);
                if (!_transfersForCurrentGameweek.ContainsKey(c.LeagueId))
                {
                    var transferForLeague = await _transfersByGameWeek.GetTransfersByGameweek(currentGameweek, c.LeagueId);
                    _transfersForCurrentGameweek.Add(c.LeagueId, transferForLeague);
                }
            }
            var latest = await _fixtureClient.GetFixturesByGameweek(currentGameweek);
            var newEvents = GetUpdatedFixtureEvents(latest, _currentGameweekFixtures);

            if (newEvents.Any())
            {
                foreach (var league in _transfersForCurrentGameweek.Keys)
                {
                    var theEventsForTheLeague = _transfersForCurrentGameweek[league];
                    var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), theEventsForTheLeague, _players, _teams);
                    await PostNewEvents(league, formattedEvents);
                }

                _currentGameweekFixtures = latest;
            }
        }

        public static IEnumerable<FixtureEvents> GetUpdatedFixtureEvents(ICollection<Fixture> latestFixtures, ICollection<Fixture> current)
        {
            if(latestFixtures == null)
                return new List<FixtureEvents>();
            
            if (current == null)
                return new List<FixtureEvents>();
            
            return latestFixtures.Where(f => f.Stats.Any()).Select(fixture =>
            {
                var oldFixture = current.FirstOrDefault(f => f.Code == fixture.Code);
                if (oldFixture != null)
                {
                    var newFixtureStats = StatHelper.DiffFixtureStats(fixture, oldFixture);

                    if (newFixtureStats.Values.Any())
                        return new FixtureEvents
                        {
                            GameScore = new GameScore
                            {
                                HomeTeamId = fixture.HomeTeamId,
                                AwayTeamId = fixture.AwayTeamId,
                                HomeTeamScore = fixture.HomeTeamScore,
                                AwayTeamScore = fixture.AwayTeamScore,
                            },
                            StatMap = newFixtureStats
                        };
                    else
                        return null;
                }

                return null;
            }).WhereNotNull();
        }

        private async Task PostNewEvents(int leagueId, List<string> events)
        {
            foreach (var s in events)
            {
                await PublishToSingleWorkspaceConnectedToLeague(async slackClient => await Task.FromResult(s), leagueId);
                await Task.Delay(500);
            }
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}
