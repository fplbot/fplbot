using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class FixtureEventsMonitor : IMonitorFixtureEvents
    {
        private readonly ILogger<FixtureEventsMonitor> _logger;
        private readonly IState _state;

        public FixtureEventsMonitor(IState state, ISlackWorkSpacePublisher publisher, ILogger<FixtureEventsMonitor> logger)
        {
            _logger = logger;
            _state = state;
            var fixtureEventHandler = new FixtureEventsHandler(publisher);
            _state.OnNewFixtureEvents += fixtureEventHandler.OnNewFixtureEvents;
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
    }
}