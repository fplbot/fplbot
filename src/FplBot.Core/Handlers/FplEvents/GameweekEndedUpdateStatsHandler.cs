using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Core.Models;
using MediatR;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    internal class GameweekEndedUpdateStatsHandler : INotificationHandler<GameweekFinished>
    {
        private readonly IMediator _mediator;
        
        public GameweekEndedUpdateStatsHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(GameweekFinished notification, CancellationToken cancellationToken)
        {
            var t1 = _mediator.Publish(new UpdateEntryStats(Gameweek: notification.Gameweek.Id), cancellationToken);
            var t2 = _mediator.Publish(new UpdateSelfishStats(Gameweek : notification.Gameweek.Id), cancellationToken);
            await Task.WhenAll(t1, t2);
        }
    }
}