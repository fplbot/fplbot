using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fpl.Workers.RecurringActions
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

            var leaguesStatusChanged = fetched.Leagues != _storedCurrent.Leagues;

            if (leaguesStatusChanged)
            {
                _logger.LogInformation($"League status changed from ${_storedCurrent.Leagues} to ${fetched.Leagues}");
                await _mediator.Publish(new LeagueStatusChanged(_storedCurrent.Leagues, fetched.Leagues), token);
            }

            _storedCurrent = fetched;
        }

        public static BonusAdded GetBonusAdded(EventStatusResponse fetched, EventStatusResponse current)
        {
            var fetchedStatus = fetched.Status;
            var currentStatus = current.Status;

            foreach (EventStatus eventStatus in fetchedStatus)
            {
                var currentEventStatus = currentStatus.FirstOrDefault(c => c.Date == eventStatus.Date);
                if (currentEventStatus?.BonusAdded == false && eventStatus.BonusAdded)
                    return new BonusAdded(eventStatus.Event, DateTime.ParseExact(eventStatus.Date,"yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo).Date);
            }

            return null;
        }

        public static PointsReady GetPointsReady(EventStatusResponse fetched, EventStatusResponse current)
        {
            var fetchedStatus = fetched.Status;
            var currentStatus = current.Status;

            foreach (EventStatus eventStatus in fetchedStatus)
            {
                var currentEventStatus = currentStatus.FirstOrDefault(c => c.Date == eventStatus.Date);
                if (currentEventStatus?.PointsStatus != EventStatusConstants.PointStatus.Ready && eventStatus.PointsStatus == EventStatusConstants.PointStatus.Ready)
                    return new PointsReady(eventStatus.Event, DateTime.ParseExact(eventStatus.Date,"yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo).Date);
            }

            return null;
        }
    }
}
