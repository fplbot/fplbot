using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class FixtureEventsHandler
    {
        private readonly IState _state;
        private readonly ISlackWorkSpacePublisher _publisher;

        public FixtureEventsHandler(IState state, ISlackWorkSpacePublisher publisher)
        {
            _state = state;
            _publisher = publisher;
        }
        
        public async Task OnNewFixtureEvents(IEnumerable<FixtureEvents> newEvents)
        {
            foreach (var team in _state.GetActiveTeams())
            {
                var context = _state.GetGameweekLeagueContext(team.TeamId);
                var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), context);
                await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, formattedEvents.ToArray());
            }
        }
    }
}