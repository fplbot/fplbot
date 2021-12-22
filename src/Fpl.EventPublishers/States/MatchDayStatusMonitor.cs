using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Fpl.EventPublishers.States;

public class MatchDayStatusMonitor
{
    private readonly IEventStatusClient _eventStatusClient;
    private readonly IMessageSession _session;
    private EventStatusResponse _storedCurrent;
    private ILogger<MatchDayStatusMonitor> _logger;

    public MatchDayStatusMonitor(IEventStatusClient eventStatusClient, IMessageSession session, ILogger<MatchDayStatusMonitor> logger)
    {
        _eventStatusClient = eventStatusClient;
        _session = session;
        _logger = logger;
    }

    public async Task EveryFiveMinutesTick(CancellationToken token)
    {
        EventStatusResponse fetched;
        try
        {
            fetched = await _eventStatusClient.GetEventStatus();
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

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
            await _session.Publish(bonusAdded);
        }

        var pointsReady = GetPointsReady(fetched, _storedCurrent);

        if (pointsReady != null)
        {
            _logger.LogInformation("Points ready!");
            await _session.Publish(pointsReady);
        }

        var leaguesStatusChanged = fetched.Leagues != _storedCurrent.Leagues;

        if (leaguesStatusChanged && fetched.Leagues == EventStatusConstants.LeaguesStatus.Updated)
        {
            _logger.LogInformation($"League status changed from ${_storedCurrent.Leagues} to ${fetched.Leagues}");
            await _session.Publish(new MatchdayLeaguesUpdated());
        }

        _storedCurrent = fetched;
    }

    private bool LogError(Exception e)
    {
        if (e is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable })
        {
            _logger.LogWarning("Game is updating");
        }
        else
        {
            _logger.LogError(e, e.Message);
        }

        return true;
    }

    private static MatchdayBonusPointsAdded GetBonusAdded(EventStatusResponse fetched, EventStatusResponse current)
    {
        var fetchedStatus = fetched.Status;
        var currentStatus = current.Status;

        foreach (EventStatus eventStatus in fetchedStatus)
        {
            var currentEventStatus = currentStatus.FirstOrDefault(c => c.Date == eventStatus.Date);
            if (currentEventStatus?.BonusAdded == false && eventStatus.BonusAdded)
                return new MatchdayBonusPointsAdded(eventStatus.Event, eventStatus.Date);
        }

        return null;
    }

    private static MatchdayMatchPointsAdded GetPointsReady(EventStatusResponse fetched, EventStatusResponse current)
    {
        var fetchedStatus = fetched.Status;
        var currentStatus = current.Status;

        foreach (EventStatus eventStatus in fetchedStatus)
        {
            var currentEventStatus = currentStatus.FirstOrDefault(c => c.Date == eventStatus.Date);
            if (currentEventStatus?.PointsStatus != EventStatusConstants.PointStatus.Ready && eventStatus.PointsStatus == EventStatusConstants.PointStatus.Ready)
                return new MatchdayMatchPointsAdded(eventStatus.Event, eventStatus.Date);
        }

        return null;
    }
}
