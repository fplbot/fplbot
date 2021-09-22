using System.Threading.Tasks;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Messaging.Contracts.Events.v1;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Core.Handlers.FplEvents
{
    internal class GameweekJustBeganUpdateStatsHandler : IHandleMessages<GameweekJustBegan>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GameweekJustBeganUpdateStatsHandler> _logger;

        public GameweekJustBeganUpdateStatsHandler(IMediator mediator, ILogger<GameweekJustBeganUpdateStatsHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(GameweekJustBegan notification, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Handling using {nameof(GameweekJustBeganUpdateStatsHandler)}");
            var t1 = _mediator.Publish(new UpdateAllEntryStats());
            var t2 = _mediator.Publish(new UpdateSelfishStats(Gameweek : notification.NewGameweek.Id));
            await Task.WhenAll(t1, t2);
        }
    }
}
