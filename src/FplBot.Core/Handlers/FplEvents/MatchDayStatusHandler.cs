using System.Threading.Tasks;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Messaging.Contracts.Events.v1;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Core.Handlers.FplEvents
{
    public class MatchDayStatusHandler :
        IHandleMessages<MatchdayLeaguesUpdated>
    {

        private readonly IMediator _mediator;
        private readonly ILogger<MatchDayStatusHandler> _logger;

        public MatchDayStatusHandler(IMediator mediator, ILogger<MatchDayStatusHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(MatchdayLeaguesUpdated notification, IMessageHandlerContext context)
        {
            await _mediator.Publish(new UpdateVerifiedEntriesCurrentGwPointsCommand());
        }
    }
}
