using System.Linq;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class InjuryUpdateHandler : IHandleMessages<InjuryUpdateOccured>
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<InjuryUpdateHandler> _logger;

        public InjuryUpdateHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ILogger<InjuryUpdateHandler> logger)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
        }

        public async Task Handle(InjuryUpdateOccured notification, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Handling player {notification.PlayersWithInjuryUpdates.Count()} status updates");
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.HasRegisteredFor(EventSubscription.InjuryUpdates))
                {
                    var filtered = notification.PlayersWithInjuryUpdates.Where(c => c.Player.IsRelevant());
                    if (filtered.Any())
                    {
                        var formatted = Formatter.FormatInjuryStatusUpdates(filtered);
                        await context.SendLocal(new PublishToSlack(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted));
                    }
                    else
                    {
                        _logger.LogInformation("All updates injuries irrelevant, so not sending any notification");
                    }
                }
            }
        }
    }
}
