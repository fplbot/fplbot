using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats
{
    public class GoalDescriber : IDescribeTaunts
    {
        public static string[] GoalJokes = new []
        {
            "Ah jeez, you transferred him out, {0} 🤣",
            "You just had to knee jerk him out, didn't you, {0}?",
            "Didn't you have that guy last week, {0}?",
            "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
        };

        public TauntType Type => TauntType.OutTransfers;

        public string[] JokePool => GoalJokes;

        public string EventDescriptionSingular => "{0} scored a goal! {1}";
        public string EventDescriptionPlural => "{0} scored {1} goals! {2}";

        public string EventEmoji => "⚽️";
    }
}
