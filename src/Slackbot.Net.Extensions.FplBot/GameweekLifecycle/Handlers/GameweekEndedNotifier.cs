using System;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekEndedNotifier : IHandleGameweekEnded
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly IFetchFplbotSetup _teamRepo;
        private readonly ITokenStore _tokenStore;
        private readonly ILeagueClient _leagueClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILogger<GameweekEndedNotifier> _logger;

        public GameweekEndedNotifier(ISlackWorkSpacePublisher publisher, 
            IFetchFplbotSetup teamsRepo, 
            ITokenStore tokenStore, 
            ILeagueClient leagueClient, 
            IGameweekClient gameweekClient, ILogger<GameweekEndedNotifier> logger)
        {
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _tokenStore = tokenStore;
            _leagueClient = leagueClient;
            _gameweekClient = gameweekClient;
            _logger = logger;
        }

        public async Task HandleGameweekEnded(int gameweek)
        {
            await _publisher.PublishToAllWorkspaces(_ => $"Gameweek {gameweek} finished.");
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);
                try
                {
                    var league = await _leagueClient.GetClassicLeague(setup.LeagueId);
                    var gameweeks = await _gameweekClient.GetGameweeks();
                    var standings = Formatter.GetStandings(league, gameweeks);
                    await _publisher.PublishUsingToken(token, _ => standings);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }
    }
}