using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.SlackClients.Http;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NextGameweekRecurringAction : GameweekRecurringActionBase
    {
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;

        public NextGameweekRecurringAction(
            IGameweekClient gwClient,
            ICaptainsByGameWeek captainsByGameweek,
            ITransfersByGameWeek transfersByGameweek,
            ILogger<NextGameweekRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder,
            IFetchFplbotSetup teamRepo) : 
            base(gwClient, logger, tokenStore, slackClientBuilder, teamRepo)
        {
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
        }

        protected override async Task DoStuffWhenNewGameweekHaveJustBegun(int newGameweek)
        {
            await PublishToAllWorkspaces(_ => Task.FromResult($"Gameweek {newGameweek}!"));

            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);
                
                var captains = await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, setup.LeagueId);
                await Publish(_ => Task.FromResult(captains), token);

                var captainsChart = await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, setup.LeagueId);
                await Publish(_ => Task.FromResult(captainsChart), token);

                var transfers = await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, setup.LeagueId);
                await Publish(_ => Task.FromResult(transfers), token);
            }
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}