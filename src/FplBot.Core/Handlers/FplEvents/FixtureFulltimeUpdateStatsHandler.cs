using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class FixtureFulltimeUpdateStatsHandler : INotificationHandler<FixturesFinished>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FixtureFulltimeUpdateStatsHandler> _logger;

        public FixtureFulltimeUpdateStatsHandler(IMediator mediator, ILogger<FixtureFulltimeUpdateStatsHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(FixturesFinished notification, CancellationToken cancellationToken)
        {
            await _mediator.Publish(new UpdateEntryStats(), cancellationToken);
        }
    }
}