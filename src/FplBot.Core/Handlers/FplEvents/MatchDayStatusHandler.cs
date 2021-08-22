using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Models;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.Handlers.FplEvents
{
    public class MatchDayStatusHandler :
        INotificationHandler<BonusAdded>,
        INotificationHandler<PointsReady>,
        INotificationHandler<LeagueStatusChanged>
    {

        private readonly IMediator _mediator;
        private readonly ILogger<MatchDayStatusHandler> _logger;

        public MatchDayStatusHandler(IMediator mediator, ILogger<MatchDayStatusHandler> logger)
        {

            _mediator = mediator;
            _logger = logger;
        }

        public Task Handle(BonusAdded notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Bonus added for matchday {notification.MatchDayDate:yyyy-MM-dd} in gw {notification.Event}");
            return Task.CompletedTask;
        }

        public Task Handle(PointsReady notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Points ready for matchday {notification.MatchDayDate:yyyy-MM-dd} in gw {notification.Event}");
            return Task.CompletedTask;
        }

        public async Task Handle(LeagueStatusChanged notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"League status changed from `{notification.prevState}` to `{notification.newState}`");
            if (notification.newState == EventStatusConstants.LeaguesStatus.Updated)
            {
                await _mediator.Publish(new UpdateVerifiedEntriesCurrentGwPointsCommand(), cancellationToken);
            }
        }
    }
}
