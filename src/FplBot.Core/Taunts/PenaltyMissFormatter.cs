using System.Collections.Generic;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Taunts
{
    public class PenaltyMissFormatter : IFormatWithTaunts
    {
        private readonly TauntyFormatter _formatter;

        public PenaltyMissFormatter(TauntData tauntData)
        {
            _formatter = new TauntyFormatter(this, tauntData);
        }
        public TauntType Type => TauntType.HasPlayerInTeam;

        public string[] JokePool => new []
        {
            "Bet you thought you were getting some points there, {0}!",
            "Isn't that guy in your team, {0}?"
        };

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
        {
            return _formatter.Format(events);
        }

        public string EventDescription => "missed a penalty!";
        public string EventEmoji => ":dizzy_face:";
    }
}
