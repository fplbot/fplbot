using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Core.Models;
using FplBot.Messaging.Contracts.Commands.v1;
using MediatR;
using NServiceBus;

namespace FplBot.Core.Handlers.FplEvents
{
    public class MatchDayStatusHandler :
        INotificationHandler<BonusAdded>,
        INotificationHandler<PointsReady>,
        INotificationHandler<LeagueStatusChanged>
    {
        private readonly IMessageSession _session;
        private readonly IMediator _mediator;

        public MatchDayStatusHandler(IMessageSession session, IMediator mediator)
        {
            _session = session;
            _mediator = mediator;
        }

        public async Task Handle(BonusAdded notification, CancellationToken cancellationToken)
        {
            await _session.SendLocal(new PublishToSlack("T0A9QSU83", "#johntest", $"Bonus added for matchday {notification.MatchDayDate:yyyy-MM-dd} in gw {notification.Event}"));
        }

        public async Task Handle(PointsReady notification, CancellationToken cancellationToken)
        {
            await _session.SendLocal(new PublishToSlack("T0A9QSU83", "#johntest", $"Points ready for matchday {notification.MatchDayDate:yyyy-MM-dd} in gw {notification.Event}"));
            await _mediator.Publish(new UpdateVerifiedEntriesCurrentGwPointsCommand(), cancellationToken);
        }

        public async Task Handle(LeagueStatusChanged notification, CancellationToken cancellationToken)
        {
            await _session.SendLocal(new PublishToSlack("T0A9QSU83", "#johntest", $"League status changed from `{notification.prevState}` to `{notification.newState}`"));
        }
    }
}
