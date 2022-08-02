using Fpl.EventPublishers.Events;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.States;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.Mediators;

/// <summary>
/// This class is wired up by MediatR, do not delete
/// </summary>
internal class LineupsHandler :
    INotificationHandler<GameweekMonitoringStarted>,
    INotificationHandler<GameweekJustBegan>,
    INotificationHandler<GameweekCurrentlyOnGoing>,
    INotificationHandler<GameweekCurrentlyFinished>
{

    private readonly ILogger<LineupsHandler> _logger;
    private readonly LineupState _matchState;

    public LineupsHandler(LineupState matchState, ILogger<LineupsHandler> logger)
    {
        _logger = logger;
        _matchState = matchState;
    }

    public Task Handle(GameweekMonitoringStarted notification, CancellationToken cancellationToken)
    {
        var initId = notification.Gameweek.Id;
        if (notification.Gameweek.IsFinished)
        {
            initId++;
        }
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekMonitoringStarted), initId.ToString()));
        _logger.LogInformation("Init {Gameweek}", initId);
        return _matchState.Reset(initId);
    }

    public Task Handle(GameweekJustBegan notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekJustBegan), notification.Gameweek.Id.ToString()));
        _logger.LogInformation("Resetting state. Gameweek {Gameweek} just began.", notification.Gameweek.Id);
        return _matchState.Reset(notification.Gameweek.Id);
    }

    public Task Handle(GameweekCurrentlyOnGoing notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekCurrentlyOnGoing), notification.Gameweek.Id.ToString()));
        _logger.LogInformation("Refreshing state for ongoing gw {Gameweek}", notification.Gameweek.Id);
        return _matchState.Refresh(notification.Gameweek.Id);
    }

    public Task Handle(GameweekCurrentlyFinished notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.AddContext(Tuple.Create(nameof(GameweekCurrentlyFinished), (notification.Gameweek.Id+1).ToString()));
        _logger.LogInformation("Refreshing state for finished gw {Gameweek}. Using next gw {NextGameweek}", notification.Gameweek.Id, notification.Gameweek.Id + 1);
        return _matchState.Refresh(notification.Gameweek.Id + 1); // monitor next gameweeks matches, since current = finished
    }
}
