using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekEndedNotifier : IHandleGameweekEnded
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly IFetchFplbotSetup _teamRepo;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILeagueClient _leagueClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILogger<GameweekEndedNotifier> _logger;

        public GameweekEndedNotifier(
            ISlackWorkSpacePublisher publisher, 
            IFetchFplbotSetup teamsRepo,
            ISlackTeamRepository slackTeamRepo,
            ILeagueClient leagueClient, 
            IGameweekClient gameweekClient, 
            ILogger<GameweekEndedNotifier> logger)
        {
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _slackTeamRepo = slackTeamRepo;
            _leagueClient = leagueClient;
            _gameweekClient = gameweekClient;
            _logger = logger;
        }

        public async Task HandleGameweekEndeded(int gameweek)
        {
            await _publisher.PublishToAllWorkspaceChannels($"Gameweek {gameweek} finished.");
            var allTeams = await _slackTeamRepo.GetAllTeamsAsync();

            foreach (var team in allTeams)
            {
                if (!team.FplBotEventSubscriptions.ContainsSubscriptionFor(EventSubscription.Standings))
                {
                    _logger.LogInformation("Team {team} hasn't subscribed for gw standings, so bypassing it", team.TeamId);
                    return;
                }

                try
                {
                    var league = await _leagueClient.GetClassicLeague((int)team.FplbotLeagueId);
                    var gameweeks = await _gameweekClient.GetGameweeks();
                    var standings = Formatter.GetStandings(league, gameweeks);
                    await _publisher.PublishToWorkspaceChannelUsingToken(team.AccessToken, standings);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }
    }
}