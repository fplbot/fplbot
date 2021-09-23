using System.Collections.Generic;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Taunts
{
    public class GoalFormatter: IFormatWithTaunts
    {
        private readonly IFormat _formatter;

        public static string[] GoalJokes = new []
        {
            "Ah jeez, you transferred him out, {0} :joy:",
            "You just had to knee jerk him out, didn't you, {0}?",
            "Didn't you have that guy last week, {0}?",
            "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
        };

        public GoalFormatter(TauntData tauntData)
        {
            _formatter = tauntData != null ? new TauntyFormatter(this, tauntData) : new RegularFormatter(this);
        }

        public TauntType Type => TauntType.OutTransfers;

        public string[] JokePool => GoalJokes;

        public string EventDescriptionSingular => "{0} scored a goal! {1}";
        public string EventDescriptionPlural => "{0} scored {1} goals! {2}";

        public string EventEmoji => ":soccer:";

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> goalEvents)
        {
            return _formatter.Format(goalEvents);
        }
    }
}
