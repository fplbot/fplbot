using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.PriceMonitoring;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    public class PriceChangedRecurringAction : IRecurringAction
    {
        private readonly PriceChangedMonitor _priceChangedMonitor;
        private readonly ISlackWorkSpacePublisher _slackWorkSpacePublisher;

        public PriceChangedRecurringAction(PriceChangedMonitor priceChangedMonitor, ISlackWorkSpacePublisher slackWorkSpacePublisher)
        {
            _priceChangedMonitor = priceChangedMonitor;
            _slackWorkSpacePublisher = slackWorkSpacePublisher;
        }
        
        public async Task Process()
        {
            var priceChanges = await _priceChangedMonitor.GetChangedPlayers();
            if (priceChanges.Players.Any())
            {
                var message = Formatter.FormatPriceChanged(priceChanges.Players, priceChanges.Teams);
                await _slackWorkSpacePublisher.PublishToAllWorkspaceChannels(message);
            }
        }

        public string Cron => Constants.CronPatterns.EveryOtherMinuteAt40SecondsSharp;
    }
}