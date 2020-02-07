using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ICaptainsByGameWeek captainsByGameweek,
            ITransfersByGameWeek transfersByGameweek,
            ILogger<NextGameweekRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder) : 
            base(options, gwClient, logger, tokenStore, slackClientBuilder)
        {
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
        }

        protected override async Task DoStuffWhenNewGameweekHaveJustBegun(int newGameweek)
        {
            await Publish(_ => Task.FromResult($"Gameweek {newGameweek}!"));

            var captains = await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek);
            await Publish(_ => Task.FromResult(captains));

            var captainsChart = await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek);
            await Publish(_ => Task.FromResult(captainsChart));

            var transfers = await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek);
            await Publish(_ => Task.FromResult(transfers));
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}