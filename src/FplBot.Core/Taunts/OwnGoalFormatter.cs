using System.Collections.Generic;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Taunts
{
    public class OwnGoalFormatter : IFormatWithTaunts
    {
        private readonly TauntyFormatter _formatter;

        public OwnGoalFormatter(TauntData tauntData)
        {
            _formatter = new TauntyFormatter(this, tauntData);
        }

        public TauntType Type => TauntType.HasPlayerInTeam;

        public string[] JokePool => new[]
        {
            "Isn't that guy in your team, {0}?",
            "That's -2pts, {0} :grimacing:",
            "Are you playing anti-fpl, {0}?"
        };

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
        {
            return _formatter.Format(events);
        }
        public string EventDescription => "scored a goal! In his own goal!";
        public string EventEmoji => ":face_palm:";
    }
}
