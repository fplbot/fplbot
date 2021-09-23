using System.Collections.Generic;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Taunts
{
    public class RedCardFormatter : IFormatWithTaunts
    {
        private readonly TauntyFormatter _formatter;

        public RedCardFormatter(TauntData tauntData)
        {
            _formatter = new TauntyFormatter(this, tauntData);
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

        public string EventDescription => "got a red card!";
        public string EventEmoji => ":red_circle:";
    }
}
