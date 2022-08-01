using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Fpl.EventPublishers.States;

internal class GameweekLifecycleMonitor
{
    private readonly IGlobalSettingsClient _gwClient;
    private readonly ILogger<GameweekLifecycleMonitor> _logger;
    private readonly IMediator _mediator;
    private readonly IMessageSession _session;

    private Gameweek _storedCurrent;

    public GameweekLifecycleMonitor(IGlobalSettingsClient gwClient, ILogger<GameweekLifecycleMonitor> logger, IMediator mediator, IMessageSession session)
    {
        _gwClient = gwClient;
        _logger = logger;
        _mediator = mediator;
        _session = session;
    }

    public async Task EveryOtherMinuteTick(CancellationToken token)
    {
        GlobalSettings globalSettings;
        try
        {
            globalSettings = await _gwClient.GetGlobalSettings();
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        var gameweeks = globalSettings.Gameweeks;
        var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);


        if (_storedCurrent == null)
        {
            _logger.LogDebug("Executing initial fetch");
            _storedCurrent = fetchedCurrent;
            if (fetchedCurrent != null)
            {
                await _mediator.Publish(new GameweekMonitoringStarted(fetchedCurrent), token);
            }
            else
            {
                var fetchedNext = gameweeks.FirstOrDefault(gw => gw.IsNext);
                _storedCurrent = fetchedNext;
                if(fetchedNext != null)
                    await _mediator.Publish(new GameweekMonitoringStarted(fetchedNext), token);
            }
        }

        if (fetchedCurrent == null)
        {
            _logger.LogDebug("No gw marked as current");
            return;
        }

        _logger.LogDebug($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");

        if (IsFirstGameweekChangingToCurrent(fetchedCurrent) || IsChangeToNewGameweek(fetchedCurrent))
        {
            await _session.Publish(new FplBot.Messaging.Contracts.Events.v1.GameweekJustBegan(new (fetchedCurrent.Id)));
            await _mediator.Publish(new GameweekJustBegan(fetchedCurrent), token);

        }
        else if (IsChangeToFinishedGameweek(fetchedCurrent))
        {
            await _session.Publish(new FplBot.Messaging.Contracts.Events.v1.GameweekFinished(new (fetchedCurrent.Id)));
            await _mediator.Publish(new GameweekFinished(fetchedCurrent), token);
        }
        else
        {
            if (!_storedCurrent.IsFinished)
            {
                await _mediator.Publish(new GameweekCurrentlyOnGoing(_storedCurrent), token);
            }
            else
            {
                await _mediator.Publish(new GameweekCurrentlyFinished(_storedCurrent), token);
            }
        }

        _storedCurrent = fetchedCurrent;

    }

    private bool IsChangeToNewGameweek(Gameweek fetchedCurrent)
    {
        return fetchedCurrent.Id > _storedCurrent.Id;
    }

    private bool IsChangeToFinishedGameweek(Gameweek fetchedCurrent)
    {
        return fetchedCurrent.Id == _storedCurrent.Id && !_storedCurrent.IsFinished && fetchedCurrent.IsFinished;
    }

    private bool IsFirstGameweekChangingToCurrent(Gameweek fetchedCurrent)
    {
        var isFirstGameweekBeginning = _storedCurrent.Id == 1 && fetchedCurrent.Id == 1;
        var isFirstGameweekChangeToCurrent = _storedCurrent.IsCurrent == false && fetchedCurrent.IsCurrent;
        return isFirstGameweekBeginning && isFirstGameweekChangeToCurrent;
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
}
