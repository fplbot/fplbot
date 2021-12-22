using Fpl.EventPublishers.Events;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.States;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.Mediators;

/// <summary>
/// This class is wired up by MediatR, do not delete
/// </summary>
internal class StateMediator :
    INotificationHandler<GameweekMonitoringStarted>,
    INotificationHandler<GameweekJustBegan>,
    INotificationHandler<GameweekCurrentlyOnGoing>,
    INotificationHandler<GameweekCurrentlyFinished>
{
    private readonly ILogger<StateMediator> _logger;
    private readonly State _state;

    public StateMediator(State state, ILogger<StateMediator> logger)
    {
        _logger = logger;
        _state = state;
    }

    public Task Handle(GameweekMonitoringStarted notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekMonitoringStarted), notification.CurrentGameweek.Id.ToString()));
        _logger.LogInformation("Init");
        return _state.Reset(notification.CurrentGameweek.Id);
    }

    public Task Handle(GameweekJustBegan notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekJustBegan), notification.Gameweek.Id.ToString()));
        _logger.LogInformation("Getting ready to rumble");
        return _state.Reset(notification.Gameweek.Id);
    }

    public Task Handle(GameweekCurrentlyOnGoing notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekCurrentlyOnGoing), notification.Gameweek.Id.ToString()));
        _logger.LogInformation("Refreshing state for ongoing gw");
        return _state.Refresh(notification.Gameweek.Id);
    }

    public Task Handle(GameweekCurrentlyFinished notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekCurrentlyFinished), notification.Gameweek.Id.ToString()));
        _logger.LogInformation("Refreshing state - finished gw");
        return _state.Refresh(notification.Gameweek.Id);
    }
}
