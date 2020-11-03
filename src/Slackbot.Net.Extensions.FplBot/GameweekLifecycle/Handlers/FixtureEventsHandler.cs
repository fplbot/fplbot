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
        private readonly ISlackWorkSpacePublisher _publisher;

        public FixtureEventsHandler(ISlackWorkSpacePublisher publisher)
        {
            _publisher = publisher;
        }
        
        public async Task OnNewFixtureEvents(GameweekLeagueContext context, IEnumerable<FixtureEvents> newEvents)
        {
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), context);
            await _publisher.PublishToWorkspace(context.SlackTeam.TeamId, context.SlackTeam.FplBotSlackChannel, formattedEvents.ToArray());
        }
    }
}