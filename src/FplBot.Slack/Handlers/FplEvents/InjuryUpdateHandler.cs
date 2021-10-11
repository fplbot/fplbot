using System.Linq;
using System.Threading.Tasks;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using FplBot.Slack.Extensions;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Slack.Handlers.FplEvents
{
    public class InjuryUpdateHandler : IHandleMessages<InjuryUpdateOccured>
    {
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<InjuryUpdateHandler> _logger;

        public InjuryUpdateHandler(ISlackTeamRepository slackTeamRepo, ILogger<InjuryUpdateHandler> logger)
        {
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
        }

        public async Task Handle(InjuryUpdateOccured notification, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Handling {notification.PlayersWithInjuryUpdates.Count()} injury updates");
            var filtered = notification.PlayersWithInjuryUpdates.Where(c => c.Player.IsRelevant());
            if (filtered.Any())
            {
                var formatted = Formatter.FormatInjuryStatusUpdates(filtered);
                var slackTeams = await _slackTeamRepo.GetAllTeams();
                foreach (var slackTeam in slackTeams)
                {
                    if (slackTeam.HasRegisteredFor(EventSubscription.InjuryUpdates))
                        await context.SendLocal(new PublishToSlack(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted));
                }
            }
            else
            {
                _logger.LogInformation("All updates injuries irrelevant, so not sending any notification");
            }
        }
    }
}
