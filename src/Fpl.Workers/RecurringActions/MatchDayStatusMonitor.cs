using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.RecurringActions
{
    public class MatchDayStatusMonitor
    {
        private readonly IEventStatusClient _eventStatusClient;
        private readonly IMediator _mediator;
        private EventStatusResponse _storedCurrent;
        private ILogger<MatchDayStatusMonitor> _logger;

        public MatchDayStatusMonitor(IEventStatusClient eventStatusClient, IMediator mediator, ILogger<MatchDayStatusMonitor> logger)
        {
            _eventStatusClient = eventStatusClient;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task EveryFiveMinutesTick(CancellationToken token)
        {
            var fetched = await _eventStatusClient.GetEventStatus();
            
            // init/ app-startup
            if (_storedCurrent == null)
            {
                _logger.LogDebug("Executing initial fetch");
                _storedCurrent = fetched;
                return;
            }
            _logger.LogInformation("Checking status");
            var bonusAdded = GetBonusAdded(fetched, _storedCurrent);

            if (bonusAdded != null)
            {
                _logger.LogInformation("Bonus added!");
                await _mediator.Publish(bonusAdded, token);
            } 
                

            var pointsReady = GetPointsReady(fetched, _storedCurrent);

            if (pointsReady != null)
            {
                _logger.LogInformation("Points ready!");
                await _mediator.Publish(pointsReady, token);
            }
        }

        public static BonusAdded GetBonusAdded(EventStatusResponse fetched, EventStatusResponse current)
        {
            DateTime utcNowDate = DateTime.UtcNow.Date;
            var today = utcNowDate.ToString("yyyy-MM-dd");
            var fetchedStatusForToday = fetched.Status.FirstOrDefault(m => m.Date == today);
            if (fetchedStatusForToday != null)
            {
                var currentStatusForMatchDay = current.Status.FirstOrDefault(m => m.Date == today);
                if (currentStatusForMatchDay?.BonusAdded == false && fetchedStatusForToday.BonusAdded)
                {
                    return new BonusAdded(Event: fetchedStatusForToday.Event, MatchDayDate: utcNowDate);
                }
            }

            return null;
        }
        
        public static PointsReady GetPointsReady(EventStatusResponse fetched, EventStatusResponse current)
        {
            DateTime utcNowDate = DateTime.UtcNow.Date;
            var today = utcNowDate.ToString("yyyy-MM-dd");
            var fetchedStatusForToday = fetched.Status.FirstOrDefault(m => m.Date == today);
            if (fetchedStatusForToday != null)
            {
                var currentStatusForMatchDay = current.Status.FirstOrDefault(m => m.Date == today);
                if (currentStatusForMatchDay?.PointsStatus != EventStatusConstants.PointStatus.Ready && fetchedStatusForToday.PointsStatus == EventStatusConstants.PointStatus.Ready)
                {
                    return new PointsReady(Event: fetchedStatusForToday.Event, MatchDayDate: utcNowDate);
                }
            }

            return null;
        }
    }

}