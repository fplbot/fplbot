using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.PriceMonitoring;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    public class PriceChangedRecurringAction : IRecurringAction
    {
        private readonly PriceChangedMonitor _priceChangedMonitor;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ISlackWorkSpacePublisher _slackWorkSpacePublisher;

        public PriceChangedRecurringAction(
            PriceChangedMonitor priceChangedMonitor,
            ISlackTeamRepository slackTeamRepo,
            ISlackWorkSpacePublisher slackWorkSpacePublisher)
        {
            _priceChangedMonitor = priceChangedMonitor;
            _slackTeamRepo = slackTeamRepo;
            _slackWorkSpacePublisher = slackWorkSpacePublisher;
        }
        
        public async Task Process()
        {
            var priceChanges = await _priceChangedMonitor.GetChangedPlayers();
            if (priceChanges.Players.Any())
            {
                var message = Formatter.FormatPriceChanged(priceChanges.Players, priceChanges.Teams);

                var allTeams = await _slackTeamRepo.GetAllTeams();
                foreach (var team in allTeams)
                {
                    if (team.FplBotEventSubscriptions.ContainsSubscriptionFor(EventSubscription.PriceChanges))
                    {
                        await _slackWorkSpacePublisher.PublishToWorkspace(team.TeamId, message);
                    }
                }
            }
        }

        public string Cron => Constants.CronPatterns.EveryOtherMinuteAt40SecondsSharp;
    }
}