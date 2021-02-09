using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.GameweekLifecycle.Handlers;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.GameweekLifecycle
{
    internal class StateEventsMonitor : IMonitorState
    {
        private readonly ILogger<StateEventsMonitor> _logger;
        private readonly IState _state;

        public StateEventsMonitor(IState state, FixtureEventsHandler fixtureEventsHandler, PriceChangeHandler priceChangeHandler, InjuryUpdateHandler statusUpdateHandler, FixtureFulltimeHandler fixtureFulltimeHandler,  ILogger<StateEventsMonitor> logger)
        {
            _logger = logger;
            _state = state;
            _state.OnNewFixtureEvents += fixtureEventsHandler.OnNewFixtureEvents;
            _state.OnPriceChanges += priceChangeHandler.OnPriceChanges;
            _state.OnInjuryUpdates += statusUpdateHandler.OnInjuryUpdates;
            _state.OnFixturesProvisionalFinished += fixtureFulltimeHandler.OnFixtureFulltime;
        }

        public async Task Initialize(int gwId)
        {
            _logger.LogInformation("Init");
            await _state.Reset(gwId);
        }

        public async Task HandleGameweekStarted(int gwId)
        {
            _logger.LogInformation("Getting ready to rumble");
            await _state.Reset(gwId);
        }

        public async Task HandleGameweekOngoing(int currentGameweek)
        {
            _logger.LogInformation("Refreshing state");
            await _state.Refresh(currentGameweek);
        }

        public async Task HandleGameweekCurrentlyFinished(int gwId)
        {
            _logger.LogInformation("Refreshing state - finished gw");
            await _state.Refresh(gwId);
        }
    }
}