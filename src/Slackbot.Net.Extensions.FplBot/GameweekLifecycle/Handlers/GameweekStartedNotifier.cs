using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.RecurringActions;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekStartedNotifier : IHandleGameweekStarted
    {
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly IFetchFplbotSetup _teamRepo;
        private readonly ITokenStore _tokenStore;

        public GameweekStartedNotifier(ICaptainsByGameWeek captainsByGameweek, 
            ITransfersByGameWeek transfersByGameweek, 
            ISlackWorkSpacePublisher publisher, 
            IFetchFplbotSetup teamsRepo, 
            ITokenStore tokenStore)
        {
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _tokenStore = tokenStore;
        }

        public async Task HandleGameweekStarted(int newGameweek)
        {
            await _publisher.PublishToAllWorkspaces($"Gameweek {newGameweek}!");
            var tokens = await _tokenStore.GetTokens();

            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);
                
                var captains = await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, setup.LeagueId);
                var captainsChart = await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, setup.LeagueId);
                var transfers = await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, setup.LeagueId);
                await _publisher.PublishUsingToken(token, captains, captainsChart, transfers);
            }
        }
    }
}