using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Core.Models;
using MediatR;

namespace FplBot.Core.Handlers.FplEvents
{
    internal class GameweekEndedUpdateStatsHandler : INotificationHandler<GameweekJustBegan>
    {
        private readonly IMediator _mediator;
        
        public GameweekEndedUpdateStatsHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(GameweekJustBegan notification, CancellationToken cancellationToken)
        {
            var t1 = _mediator.Publish(new UpdateAllEntryStats(), cancellationToken);
            var t2 = _mediator.Publish(new UpdateSelfishStats(Gameweek : notification.Gameweek.Id), cancellationToken);
            await Task.WhenAll(t1, t2);
        }
    }
}