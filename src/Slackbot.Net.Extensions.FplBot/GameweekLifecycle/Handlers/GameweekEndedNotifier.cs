using System;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

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

        public async Task HandleGameweekEndeded(int gameweek)
        {
            await _publisher.PublishToAllWorkspaceChannels($"Gameweek {gameweek} finished.");
            var teams = await _teamRepo.GetAllTeamsAsync();
            foreach (var team in teams)
            {
                try
                {
                    var league = await _leagueClient.GetClassicLeague((int)team.FplbotLeagueId);
                    var gameweeks = await _gameweekClient.GetGameweeks();
                    var standings = Formatter.GetStandings(league, gameweeks);
                    await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, standings);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }
    }
}