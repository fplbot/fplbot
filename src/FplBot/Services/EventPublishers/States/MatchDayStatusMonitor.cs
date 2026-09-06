using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.States;

public class MatchDayStatusMonitor
{
    private readonly IEventStatusClient _eventStatusClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private EventStatusResponse? _storedCurrent;
    private ILogger<MatchDayStatusMonitor> _logger;

    public MatchDayStatusMonitor(IEventStatusClient eventStatusClient, IServiceScopeFactory scopeFactory, ILogger<MatchDayStatusMonitor> logger)
    {
        _eventStatusClient = eventStatusClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task EveryFiveMinutesTick(CancellationToken token)
    {
        EventStatusResponse? fetched;
        try
        {
            fetched = await _eventStatusClient.GetEventStatus();
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        if (fetched == null)
            return;

        // init/ app-startup
        if (_storedCurrent == null)
        {
            _logger.LogDebug("Executing initial fetch");
            _storedCurrent = fetched;
            return;
        }

        _logger.LogInformation("Checking status");
        var bonusAdded = GetBonusAdded(fetched, _storedCurrent);
        var pointsReady = GetPointsReady(fetched, _storedCurrent);
        var leaguesStatusChanged = fetched.Leagues != _storedCurrent.Leagues && fetched.Leagues == EventStatusConstants.LeaguesStatus.Updated;

        if (bonusAdded != null || pointsReady != null || leaguesStatusChanged)
        {
            using var scope = _scopeFactory.CreateScope();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            if (bonusAdded != null)
            {
                _logger.LogInformation("Bonus added!");
                await publish.Publish(bonusAdded);
            }

            if (pointsReady != null)
            {
                _logger.LogInformation("Points ready!");
                await publish.Publish(pointsReady);
            }

            if (leaguesStatusChanged)
            {
                _logger.LogInformation($"League status changed from ${_storedCurrent.Leagues} to ${fetched.Leagues}");
                await publish.Publish(new MatchdayLeaguesUpdated());
            }
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

    private static MatchdayBonusPointsAdded? GetBonusAdded(EventStatusResponse fetched, EventStatusResponse current)
    {
        var fetchedStatus = fetched.Status;
        var currentStatus = current.Status;

        foreach (EventStatus eventStatus in fetchedStatus)
        {
            var currentEventStatus = currentStatus.FirstOrDefault(c => c.Date == eventStatus.Date);
            if (currentEventStatus?.BonusAdded == false && eventStatus.BonusAdded)
                return new MatchdayBonusPointsAdded(eventStatus.Event, eventStatus.Date ?? string.Empty);
        }

        return null;
    }

    private static MatchdayMatchPointsAdded? GetPointsReady(EventStatusResponse fetched, EventStatusResponse current)
    {
        var fetchedStatus = fetched.Status;
        var currentStatus = current.Status;

        foreach (EventStatus eventStatus in fetchedStatus)
        {
            var currentEventStatus = currentStatus.FirstOrDefault(c => c.Date == eventStatus.Date);
            if (currentEventStatus?.PointsStatus != EventStatusConstants.PointStatus.Ready && eventStatus.PointsStatus == EventStatusConstants.PointStatus.Ready)
                return new MatchdayMatchPointsAdded(eventStatus.Event, eventStatus.Date ?? string.Empty);
        }

        return null;
    }
}
