using System;
using System.Collections.Generic;
using System.Linq;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers.Formatting.Taunts
{
    internal class TauntyFormatter : IFormat
    {
        private readonly IFormatWithTaunts _formatter;
        private readonly TauntData _tauntData;

        public TauntyFormatter(IFormatWithTaunts formatter, TauntData tauntData)
        {
            _formatter = formatter;
            _tauntData = tauntData;
        }

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> goalEvents)
        {
            return goalEvents.GroupBy(g => g.Player).Select(g =>
            {
                var message = string.Format(_formatter.EventDescriptionSingular, $"{g.Key.FirstName} {g.Key.SecondName}", _formatter.EventEmoji);
                if (g.Count() > 1)
                {
                    var multipleEmojis = String.Concat(Enumerable.Repeat(_formatter.EventEmoji, g.Count()));
                    message = string.Format(_formatter.EventDescriptionPlural, $"{g.Key.FirstName} {g.Key.SecondName}", g.Count(), multipleEmojis);
                }
                if (g.Any(g => g.IsRemoved))
                {
                    message = $"~{message.TrimEnd()}~ (VAR? :shrug:)";
                }
                else
                {
                    var tauntibleEntries = _tauntData.GetTauntibleEntries(g.Key, _formatter.Type);
                    var append = tauntibleEntries.Any() ? $" {string.Format(_formatter.JokePool.GetRandom(), string.Join(", ", tauntibleEntries))}" : null;
                    message += append;
                }

                return message;

            });
        }
    }
}
