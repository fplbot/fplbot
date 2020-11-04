using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class StateEventsMonitor : IMonitorState
    {
        private readonly ILogger<StateEventsMonitor> _logger;
        private readonly IState _state;

        public StateEventsMonitor(IState state, ISlackWorkSpacePublisher publisher, ILogger<StateEventsMonitor> logger)
        {
            _logger = logger;
            _state = state;
            var fixtureEventHandler = new FixtureEventsHandler(publisher);
            var priceChangeHandler = new PriceChangeHandler(publisher);
            _state.OnNewFixtureEvents += fixtureEventHandler.OnNewFixtureEvents;
            _state.OnPriceChanges += priceChangeHandler.OnPriceChanges;
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