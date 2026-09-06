using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

public class MatchDayStatusMonitor(
    IEventStatusClient eventStatusClient,
    IServiceScopeFactory scopeFactory,
    ILogger<MatchDayStatusMonitor> logger)
{
    private EventStatusResponse? _storedCurrent;

    public async Task EveryFiveMinutesTick(CancellationToken token)
    {
        EventStatusResponse? fetched;
        try
        {
            fetched = await eventStatusClient.GetEventStatus(token);
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
            logger.LogDebug("Executing initial fetch");
            _storedCurrent = fetched;
            return;
        }

        logger.LogInformation("Checking status");
        var bonusAdded = GetBonusAdded(fetched, _storedCurrent);
        var pointsReady = GetPointsReady(fetched, _storedCurrent);
        var leaguesStatusChanged = fetched.Leagues != _storedCurrent.Leagues && fetched.Leagues == EventStatusConstants.LeaguesStatus.Updated;

        if (bonusAdded != null || pointsReady != null || leaguesStatusChanged)
        {
            using var scope = scopeFactory.CreateScope();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            if (bonusAdded != null)
            {
                logger.LogInformation("Bonus added!");
                await publish.Publish(bonusAdded);
            }

            if (pointsReady != null)
            {
                logger.LogInformation("Points ready!");
                await publish.Publish(pointsReady);
            }

            if (leaguesStatusChanged)
            {
                logger.LogInformation($"League status changed from ${_storedCurrent.Leagues} to ${fetched.Leagues}");
                await publish.Publish(new MatchdayLeaguesUpdated());
            }
        }

        _storedCurrent = fetched;
    }

    private bool LogError(Exception e)
    {
        if (e is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable })
        {
            logger.LogWarning("Game is updating");
        }
        else
        {
            logger.LogError(e, e.Message);
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
