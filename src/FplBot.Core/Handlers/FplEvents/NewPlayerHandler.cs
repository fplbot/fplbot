using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.Handlers.FplEvents
{
    public class NewPlayerHandler : INotificationHandler<NewPlayersRegistered>
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<NewPlayerHandler> _logger;

        public NewPlayerHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ILogger<NewPlayerHandler> logger)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
        }

        public async Task Handle(NewPlayersRegistered notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Handling {notification.NewPlayers.Count()} new players");
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.NewPlayers))
                {
                    var formatted = Formatter.FormatNewPlayers(notification.NewPlayers);
                    await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);
                }
            }
        }
    }
}
