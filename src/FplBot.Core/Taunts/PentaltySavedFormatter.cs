using System.Collections.Generic;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Taunts
{
    public class PentaltySavedFormatter : IFormatWithTaunts
    {
        private readonly IFormat _formatter;

        public PentaltySavedFormatter(TauntData tauntData)
        {
            _formatter = tauntData != null ? new TauntyFormatter(this, tauntData) : new RegularFormatter(this);
        }

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
        {
            return _formatter.Format(events);
        }

        public TauntType Type => TauntType.OutTransfers;

        public string[] JokePool => new []
        {
            "Ah jeez, you transferred him out, {0} :joy:",
            "You just had to knee jerk him out, didn't you, {0}?",
            "Didn't you have that guy last week, {0}?",
            "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
        };

        public string EventDescriptionSingular => "{0} saved a penalty! {1}";
        public string EventDescriptionPlural => "{0} saved {1} penalties! {2}";
        public string EventEmoji => ":man-cartwheeling:";
    }
}
