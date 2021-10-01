using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.GameweekLifecycle
{
    /// <summary>
    /// This class is wired up by MediatR, do not delete
    /// </summary>
    internal class StateEventsHandler :
        INotificationHandler<GameweekMonitoringStarted>,
        INotificationHandler<GameweekJustBegan>,
        INotificationHandler<GameweekCurrentlyOnGoing>,
        INotificationHandler<GameweekCurrentlyFinished>
    {
        private readonly ILogger<StateEventsHandler> _logger;
        private readonly State _state;

        public StateEventsHandler(State state, ILogger<StateEventsHandler> logger)
        {
            _logger = logger;
            _state = state;
        }

        public Task Handle(GameweekMonitoringStarted notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Init");
            return _state.Reset(notification.CurrentGameweek.Id);
        }

        public Task Handle(GameweekJustBegan notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting ready to rumble");
            return _state.Reset(notification.Gameweek.Id);
        }

        public Task Handle(GameweekCurrentlyOnGoing notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refreshing state for ongoing gw");
            return _state.Refresh(notification.Gameweek.Id);
        }

        public Task Handle(GameweekCurrentlyFinished notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refreshing state - finished gw");
            return _state.Refresh(notification.Gameweek.Id);
        }
    }
}
