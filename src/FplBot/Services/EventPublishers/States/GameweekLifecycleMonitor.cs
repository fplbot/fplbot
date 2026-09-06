using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

internal class GameweekLifecycleMonitor(
    IGlobalSettingsClient gwClient,
    ILogger<GameweekLifecycleMonitor> logger,
    IServiceScopeFactory scopeFactory,
    IFixtureState fixtureState,
    ILineupState lineupState)
{
    private Gameweek? _storedCurrent;

    public async Task EveryOtherMinuteTick(CancellationToken token)
    {
        GlobalSettings? globalSettings;
        try
        {
            globalSettings = await gwClient.GetGlobalSettings();
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        if (globalSettings == null)
        {
            return;
        }

        var gameweeks = globalSettings.Gameweeks;
        var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
        var fetchedNext = gameweeks.FirstOrDefault(gw => gw.IsNext);

        if (_storedCurrent == null)
        {
            logger.LogDebug("Executing initial fetch");
            _storedCurrent = fetchedCurrent;
            if (fetchedCurrent != null)
            {
                await fixtureState.Reset(fetchedCurrent.Id);
                await lineupState.Reset(fetchedCurrent.IsFinished ? fetchedCurrent.Id + 1 : fetchedCurrent.Id);
                return;
            }

            _storedCurrent = fetchedNext;
            if (fetchedNext != null)
            {
                await fixtureState.Reset(fetchedNext.Id);
                await lineupState.Reset(fetchedNext.IsFinished ? fetchedNext.Id + 1 : fetchedNext.Id);
                return;
            }
        }

        logger.LogDebug(
            $"Stored: {_storedCurrent?.Id} & FetchedCurrent: {fetchedCurrent?.Id} & FetchedNext:{fetchedNext?.Id}");

        if (fetchedCurrent == null)
        {
            if (fetchedNext != null)
            {
                logger.LogDebug("No gw marked as current. Using next");
                fetchedCurrent = fetchedNext;
            }
            else
            {
                logger.LogDebug("No gw marked as current or next. Skipping");
                return;
            }
        }

        if (IsFirstGameweekChangingToCurrent(fetchedCurrent) || IsChangeToNewGameweek(fetchedCurrent))
        {
            using (var scope = scopeFactory.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
                    .Publish(new GameweekJustBegan(new NewGameweek(fetchedCurrent.Id)));
            }

            await fixtureState.Reset(fetchedCurrent.Id);
            await lineupState.Reset(fetchedCurrent.Id);
            _storedCurrent = fetchedCurrent;
            return;
        }

        if (IsChangeToFinishedGameweek(fetchedCurrent))
        {
            using (var scope = scopeFactory.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
                    .Publish(new GameweekFinished(new FinishedGameweek(fetchedCurrent.Id)), token);
            }

            _storedCurrent = fetchedCurrent;
            return;
        }

        if (!_storedCurrent!.IsFinished && _storedCurrent.IsCurrent)
        {
            await fixtureState.Refresh(_storedCurrent.Id);
            await lineupState.Refresh(_storedCurrent.Id);
            _storedCurrent = fetchedCurrent;
            return;
        }

        if (_storedCurrent.IsFinished && _storedCurrent.IsCurrent)
        {
            await fixtureState.Refresh(_storedCurrent.Id);
            if (_storedCurrent.Id < 38)
            {
                await lineupState.Refresh(_storedCurrent.Id + 1);
            }

            lineupState.LogState();
            _storedCurrent = fetchedCurrent;
            return;
        }

        if (_storedCurrent.IsNext && _storedCurrent.Id == 1)
        {
            await lineupState.Refresh(1);
            _storedCurrent = fetchedCurrent;
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
        var isFirstGameweekChangeToCurrent = !_storedCurrent.IsCurrent && fetchedCurrent.IsCurrent;
        return isFirstGameweekBeginning && isFirstGameweekChangeToCurrent;
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
}
