using System.Threading.Tasks;
using Fpl.Client.Abstractions;
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

        public GameweekEndedNotifier(ISlackWorkSpacePublisher publisher, 
            IFetchFplbotSetup teamsRepo, 
            ITokenStore tokenStore, 
            ILeagueClient leagueClient, 
            IGameweekClient gameweekClient)
        {
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _tokenStore = tokenStore;
            _leagueClient = leagueClient;
            _gameweekClient = gameweekClient;
        }

        public async Task HandleGameweekEndeded(int gameweek)
        {
            await _publisher.PublishToAllWorkspaces($"Gameweek {gameweek} finished.");
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);
                var league = await _leagueClient.GetClassicLeague(setup.LeagueId);
                var gameweeks = await _gameweekClient.GetGameweeks();
                var standings = Formatter.GetStandings(league, gameweeks);
                await _publisher.PublishUsingToken(token, standings);
            }
        }
    }
}