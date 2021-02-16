using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.GameweekLifecycle
{
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

        Task INotificationHandler<GameweekMonitoringStarted>.Handle(GameweekMonitoringStarted notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Init");
            return _matchState.Reset(notification.CurrentGameweek.Id);
        }
        
        public Task Handle(GameweekJustBegan notification, CancellationToken cancellationToken)
        {  
            _logger.LogInformation("Resetting state");
            return _matchState.Reset(notification.Gameweek.Id);
        }
        
        public Task Handle(GameweekCurrentlyOnGoing notification, CancellationToken cancellationToken)
        {     
            _logger.LogInformation("Refreshing state for ongoing gw");
            return _matchState.Refresh(notification.Gameweek.Id);
        }

        public Task Handle(GameweekCurrentlyFinished notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refreshing state for finished gw");
            return _matchState.Refresh(notification.Gameweek.Id + 1); // monitor next gameweeks matches, since current = finished 
        }   
    }
}