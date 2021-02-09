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
    internal class GameweekLifecycleMonitor
    {
        private readonly IGameweekClient _gwClient;
        private readonly ILogger<GameweekLifecycleMonitor> _logger;
        private readonly IMediator _mediator;

        private Gameweek _storedCurrent;

        public GameweekLifecycleMonitor(IGameweekClient gwClient, ILogger<GameweekLifecycleMonitor> logger, IMediator mediator)
        {
            _gwClient = gwClient;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task EveryOtherMinuteTick(CancellationToken token)
        {
            var gameweeks = await _gwClient.GetGameweeks();
            var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
            if (_storedCurrent == null)
            {
                _logger.LogDebug("Executing initial fetch");
                _storedCurrent = fetchedCurrent;
                if (fetchedCurrent != null)
                {
                    await _mediator.Publish(new GameweekMonitoringStarted(fetchedCurrent), token);
                }
            }

            if (fetchedCurrent == null)
            {
                _logger.LogDebug("No gw marked as current");
                return;
            }

            _logger.LogDebug($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");

            if (IsChangeToNewGameweek(fetchedCurrent) || IsFirstGameweekChangingToCurrent(fetchedCurrent))
            {
                await _mediator.Publish(new GameweekJustBegan(fetchedCurrent), token);
                
            }
            else if (IsChangeToFinishedGameweek(fetchedCurrent))
            {
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
    }
}