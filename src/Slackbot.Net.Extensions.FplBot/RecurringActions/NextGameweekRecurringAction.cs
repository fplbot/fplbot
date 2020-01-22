using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NextGameweekRecurringAction : GameweekRecurringActionBase
    {
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;

        public NextGameweekRecurringAction(
            ITransfersByGameWeek transfersByGameweek,
            ICaptainsByGameWeek captainsByGameweek,
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            IEnumerable<IPublisher> publishers,
            ILogger<NextGameweekRecurringAction> logger) : 
            base(options, gwClient, publishers, logger)
        {
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
        }

        protected override async Task DoStuffWhenNewGameweekHaveJustBegun(int newGameweek)
        {
            await Publish($"Gameweek {newGameweek}!");

            var captains = await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek);
            await Publish(captains);

            var transfers = await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek);
            await Publish(transfers);
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}