using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using MassTransit;

namespace Fpl.EventPublishers.States;

internal class GameweekLifecycleMonitor
{
    private readonly IGlobalSettingsClient _gwClient;
    private readonly ILogger<GameweekLifecycleMonitor> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFixtureState _fixtureState;
    private readonly ILineupState _lineupState;

    private Gameweek? _storedCurrent;

    public GameweekLifecycleMonitor(IGlobalSettingsClient gwClient, ILogger<GameweekLifecycleMonitor> logger, IServiceScopeFactory scopeFactory, IFixtureState fixtureState, ILineupState lineupState)
    {
        _gwClient = gwClient;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _fixtureState = fixtureState;
        _lineupState = lineupState;
    }

    public async Task EveryOtherMinuteTick(CancellationToken token)
    {
        GlobalSettings? globalSettings;
        try
        {
            globalSettings = await _gwClient.GetGlobalSettings();
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        if (globalSettings == null)
            return;

        var gameweeks = globalSettings.Gameweeks;
        var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
        var fetchedNext = gameweeks.FirstOrDefault(gw => gw.IsNext);

        if (_storedCurrent == null)
        {
            _logger.LogDebug("Executing initial fetch");
            _storedCurrent = fetchedCurrent;
            if (fetchedCurrent != null)
            {
                await _fixtureState.Reset(fetchedCurrent.Id);
                await _lineupState.Reset(fetchedCurrent.IsFinished ? fetchedCurrent.Id + 1 : fetchedCurrent.Id);
                return;
            }
            else
            {
                _storedCurrent = fetchedNext;
                if (fetchedNext != null)
                {
                    await _fixtureState.Reset(fetchedNext.Id);
                    await _lineupState.Reset(fetchedNext.IsFinished ? fetchedNext.Id + 1 : fetchedNext.Id);
                    return;
                }
            }
        }

        _logger.LogDebug($"Stored: {_storedCurrent?.Id} & FetchedCurrent: {fetchedCurrent?.Id} & FetchedNext:{fetchedNext?.Id}");

        if (fetchedCurrent == null)
        {
            if (fetchedNext != null)
            {
                _logger.LogDebug("No gw marked as current. Using next");
                fetchedCurrent = fetchedNext;
            }
            else
            {
                _logger.LogDebug("No gw marked as current or next. Skipping");
                return;
            }
        }

        if (IsFirstGameweekChangingToCurrent(fetchedCurrent) || IsChangeToNewGameweek(fetchedCurrent))
        {
            using (var scope = _scopeFactory.CreateScope())
                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(new FplBot.Messaging.Contracts.Events.v1.GameweekJustBegan(new(fetchedCurrent.Id)));
            await _fixtureState.Reset(fetchedCurrent.Id);
            await _lineupState.Reset(fetchedCurrent.Id);
            _storedCurrent = fetchedCurrent;
            return;
        }

        if (IsChangeToFinishedGameweek(fetchedCurrent))
        {
            using (var scope = _scopeFactory.CreateScope())
                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(new FplBot.Messaging.Contracts.Events.v1.GameweekFinished(new(fetchedCurrent.Id)));
            _storedCurrent = fetchedCurrent;
            return;
        }

        if (!_storedCurrent!.IsFinished && _storedCurrent.IsCurrent)
        {
            await _fixtureState.Refresh(_storedCurrent.Id);
            await _lineupState.Refresh(_storedCurrent.Id);
            _storedCurrent = fetchedCurrent;
            return;
        }

        if (_storedCurrent.IsFinished && _storedCurrent.IsCurrent)
        {
            await _fixtureState.Refresh(_storedCurrent.Id);
            if (_storedCurrent.Id < 38)
                await _lineupState.Refresh(_storedCurrent.Id + 1);
            _lineupState.LogState();
            _storedCurrent = fetchedCurrent;
            return;
        }

        if (_storedCurrent.IsNext && _storedCurrent.Id == 1)
        {
            await _lineupState.Refresh(1);
            _storedCurrent = fetchedCurrent;
            return;
        }
    }

    private bool IsChangeToNewGameweek(Gameweek fetchedCurrent)
    {
        return fetchedCurrent.Id > _storedCurrent!.Id;
    }

    private bool IsChangeToFinishedGameweek(Gameweek fetchedCurrent)
    {
        return fetchedCurrent.Id == _storedCurrent!.Id && !_storedCurrent.IsFinished && fetchedCurrent.IsFinished;
    }

    private bool IsFirstGameweekChangingToCurrent(Gameweek fetchedCurrent)
    {
        var isFirstGameweekBeginning = _storedCurrent!.Id == 1 && fetchedCurrent.Id == 1;
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
