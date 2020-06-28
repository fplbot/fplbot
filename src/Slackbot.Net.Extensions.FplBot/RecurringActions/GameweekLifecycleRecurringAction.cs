using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Handlers;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    public class GameweekLifecycleRecurringAction : IRecurringAction
    {
        private readonly IGameweekClient _gwClient;
        private readonly ILogger<GameweekLifecycleRecurringAction> _logger;
        private readonly IGameweekMonitorOrchestrator _orchestrator;
        private Gameweek _storedCurrent;

        public GameweekLifecycleRecurringAction(
            IGameweekClient gwClient,
            ILogger<GameweekLifecycleRecurringAction> logger,
            IGameweekMonitorOrchestrator orchestrator)
        {
            _gwClient = gwClient;
            _logger = logger;
            _orchestrator = orchestrator;
        }
        
        public string Cron => Constants.CronPatterns.EveryMinute;

        public async Task Process()
        {
            var gameweeks = await _gwClient.GetGameweeks();
            var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
            if (_storedCurrent == null)
            {
                _logger.LogDebug("Executeing initial fetch");
                _storedCurrent = fetchedCurrent;
                if (fetchedCurrent != null)
                {
                    await _orchestrator.Initialize(fetchedCurrent.Id);
                }
            }

            if (fetchedCurrent == null)
            {
                _logger.LogDebug("No gw marked as current");
                return;
            }

            _logger.LogDebug($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");

            if (IsChangeToNewGameweek(fetchedCurrent))
            {
                await _orchestrator.GameweekJustBegan(fetchedCurrent.Id);
            }
            else if (IsChangeToFinishedGameweek(fetchedCurrent))
            {
                await _orchestrator.GameweekJustEnded(fetchedCurrent.Id);
            }
            else
            {
                if(!_storedCurrent.IsFinished)
                    await _orchestrator.GameweekIsCurrentlyOngoing(_storedCurrent.Id);
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
    }
}
