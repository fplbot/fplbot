using FplBot.Messaging.Contracts.Events.v1;
using FplBot.VerifiedEntries.InternalCommands;
using MassTransit;
using MediatR;

namespace FplBot.WebApi.Handlers.Commands;

internal class GameweekJustBeganUpdateStatsHandler : IConsumer<GameweekJustBegan>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GameweekJustBeganUpdateStatsHandler> _logger;

    public GameweekJustBeganUpdateStatsHandler(IMediator mediator, ILogger<GameweekJustBeganUpdateStatsHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        _logger.LogInformation($"Handling using {nameof(GameweekJustBeganUpdateStatsHandler)}");
        var t1 = _mediator.Publish(new UpdateAllEntryStats());
        var t2 = _mediator.Publish(new UpdateSelfishStats(Gameweek: context.Message.NewGameweek.Id));
        await Task.WhenAll(t1, t2);
    }
}
