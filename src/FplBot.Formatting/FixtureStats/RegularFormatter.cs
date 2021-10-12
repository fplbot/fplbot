using System;
using System.Collections.Generic;
using System.Linq;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats
{
    internal class RegularFormatter : IFormat
    {
        private readonly IFormatEvents _formatter;

        public RegularFormatter(IFormatEvents formatter)
        {
            _formatter = formatter;
        }

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
        {
            return events.GroupBy(g => g.Player).Select( g =>
            {
                var message = string.Format(_formatter.EventDescriptionSingular, $"{g.Key.FirstName} {g.Key.SecondName}", _formatter.EventEmoji);
                if (g.Count() > 1)
                {
                    var multipleEmojis = String.Concat(Enumerable.Repeat(_formatter.EventEmoji, g.Count()));
                    message = string.Format(_formatter.EventDescriptionPlural, $"{g.Key.FirstName} {g.Key.SecondName} {multipleEmojis}", g.Count(), _formatter.EventEmoji);
                }

                if (g.Any(g => g.IsRemoved))
                {
                    message = $"~{message.TrimEnd()}~ (VAR? :shrug:)";
                }
                return message;

            });
        }
    }
}
