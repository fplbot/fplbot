using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;

namespace FplBot.Slack.Helpers.Formatting.FixtureStats
{
    public class RedCardFormatter : IFormatWithTaunts
    {
        private readonly IFormat _formatter;

        public RedCardFormatter(TauntData tauntData)
        {
            _formatter = tauntData != null ? new TauntyFormatter(this, tauntData) : new RegularFormatter(this);
        }
        public TauntType Type => TauntType.InTransfers;

        public string[] JokePool => new []
        {
            "Smart move bringing him in, {0} :upside_down_face:",
            "Didn't you transfer him in this week, {0}? :japanese_ogre:",
            "Maybe you should have waited a couple of more weeks before knee jerking him in, {0}?"
        };

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
        {
            return _formatter.Format(events);
        }

        public string EventDescriptionSingular => "{0} got a red card! {1}";
        public string EventDescriptionPlural => "{0} got {1} red cards!? {2}";

        public string EventEmoji => ":red_circle:";
    }
}
