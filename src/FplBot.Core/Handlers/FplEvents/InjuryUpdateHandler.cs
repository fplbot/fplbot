using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Data;
using Fpl.Data.Abstractions;
using Fpl.Data.Models;
using Fpl.Data.Repositories;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class InjuryUpdateHandler : INotificationHandler<InjuryUpdateOccured>
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

        public async Task Handle(InjuryUpdateOccured notification, CancellationToken cancellationToken)
        {    
            _logger.LogInformation($"Handling player {notification.PlayersWithInjuryUpdates.Count()} status updates");
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.InjuryUpdates))
                {
                    var filtered = notification.PlayersWithInjuryUpdates.Where(c => c.ToPlayer.IsRelevant());
                    if (filtered.Any())
                    {
                        var formatted = Formatter.FormatInjuryStatusUpdates(filtered);
                        await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);
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