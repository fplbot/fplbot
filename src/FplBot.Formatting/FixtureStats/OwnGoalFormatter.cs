using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats
{
    public class OwnGoalFormatter : IFormatWithTaunts
    {
        private readonly IFormat _formatter;

        public OwnGoalFormatter(TauntData tauntData)
        {
            _formatter = tauntData != null ? new TauntyFormatter(this, tauntData) : new RegularFormatter(this);
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
        public string EventDescriptionSingular => "{0} scored a goal! In his own goal! {1}";
        public string EventDescriptionPlural => "{0} scored {1} own goals! {2}";
        public string EventEmoji => ":face_palm:";
    }
}
