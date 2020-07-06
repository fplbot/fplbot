using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class FixtureEventsMonitor : IMonitorFixtureEvents
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILogger<FixtureEventsMonitor> _logger;
        private readonly IState _state;

        public FixtureEventsMonitor(IState state, ISlackWorkSpacePublisher publisher, ILogger<FixtureEventsMonitor> logger)
        {
            _publisher = publisher;
            _logger = logger;
            _state = state;
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
            var newEvents = await _state.Refresh(currentGameweek);

            if (newEvents.Any())
            {
                _logger.LogInformation("New events!");
                foreach (var team in _state.GetActiveTeams())
                {
                    var context = _state.GetGameweekLeagueContext(team.TeamId);
                    var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), context);
                    await _publisher.PublishToWorkspaceChannelUsingToken(team.AccessToken, formattedEvents.ToArray());
                }
            }
        }
    }
}