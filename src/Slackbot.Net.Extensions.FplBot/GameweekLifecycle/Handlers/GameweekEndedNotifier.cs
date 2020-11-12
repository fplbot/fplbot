using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekEndedNotifier : IHandleGameweekEnded
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILeagueClient _leagueClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILogger<GameweekEndedNotifier> _logger;
        private readonly ISlackTeamRepository _teamRepo;

        public GameweekEndedNotifier(ISlackWorkSpacePublisher publisher, 
            ISlackTeamRepository teamsRepo,
            ILeagueClient leagueClient, 
            IGameweekClient gameweekClient, 
            ILogger<GameweekEndedNotifier> logger)
        {
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _leagueClient = leagueClient;
            _gameweekClient = gameweekClient;
            _logger = logger;
        }

        public async Task HandleGameweekEnded(int gameweek)
        {
            var gameweeks = await _gameweekClient.GetGameweeks();
            var gw = gameweeks.SingleOrDefault(g => g.Id == gameweek);
            if (gw == null)
            {
                _logger.LogError("Found no gameweek with id {id}", gameweek);
                return;
            }

            var teams = await _teamRepo.GetAllTeams();
            foreach (var team in teams)
            {
                if (!team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Standings))
                {
                    _logger.LogInformation("Team {team} hasn't subscribed for gw standings, so bypassing it", team.TeamId);
                    return;
                }

                try
                {
                    var league = await _leagueClient.GetClassicLeague((int)team.FplbotLeagueId);

                    var slackTeam = await _teamRepo.GetTeam("T0A9QSU83");
                    if (slackTeam.FplBotSlackChannel == "#fpltest")
                    {
                        var intro = Formatter.FormatGameweekFinished(gw, league);
                        var standings = Formatter.GetStandings(league, gw);
                        var topThree = Formatter.GetTopThreeGameweekEntries(league, gw);
                        var worst = Formatter.GetWorstGameweekEntry(league, gw);

                        await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, intro, standings, topThree, worst);
                    }
                    else
                    {
                        var standings = Formatter.GetStandings(league, gw);
                        await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, $"Gameweek {gameweek} finished.", standings);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }
    }
}