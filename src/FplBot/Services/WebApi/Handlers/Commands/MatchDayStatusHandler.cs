using FplBot.Messaging.Contracts.Events.v1;
using FplBot.VerifiedEntries.InternalCommands;
using MassTransit;
using MediatR;

namespace FplBot.WebApi.Handlers.Commands;

public class MatchDayStatusHandler : IConsumer<MatchdayLeaguesUpdated>
{
    private readonly IMediator _mediator;
    private readonly ILogger<MatchDayStatusHandler> _logger;

    public MatchDayStatusHandler(IMediator mediator, ILogger<MatchDayStatusHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MatchdayLeaguesUpdated> context)
    {
        await _mediator.Publish(new UpdateVerifiedEntriesCurrentGwPointsCommand());
    }
}
