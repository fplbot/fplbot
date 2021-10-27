using System;
using System.Collections.Generic;
using System.Linq;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats
{
    internal class TauntyFormatter : IFormat
    {
        private readonly IDescribeTaunts _describer;
        private readonly TauntData _tauntData;
        private readonly FormattingType _formattingType;

        public TauntyFormatter(IDescribeTaunts describer, TauntData tauntData, FormattingType formattingType)
        {
            _describer = describer;
            _tauntData = tauntData;
            _formattingType = formattingType;
        }

        public IEnumerable<string> Format(IEnumerable<PlayerEvent> goalEvents)
        {
            return goalEvents.GroupBy(g => g.Player).Select(g =>
            {
                var message = string.Format(_describer.EventDescriptionSingular, $"{g.Key.FirstName} {g.Key.SecondName}", _describer.EventEmoji);
                if (g.Count() > 1)
                {
                    var multipleEmojis = String.Concat(Enumerable.Repeat(_describer.EventEmoji, g.Count()));
                    message = string.Format(_describer.EventDescriptionPlural, $"{g.Key.FirstName} {g.Key.SecondName}", g.Count(), multipleEmojis);
                }
                if (g.Any(g => g.IsRemoved))
                {
                    message = $"{StrikeThrough()}{message.TrimEnd()}{StrikeThrough()} (VAR? 🤷‍♀️)";
                }
                else
                {
                    var tauntibleEntries = _tauntData.GetTauntibleEntries(g.Key, _describer.Type);
                    var append = tauntibleEntries.Any() ? $" {string.Format(_describer.JokePool.GetRandom(), string.Join(", ", tauntibleEntries))}" : null;
                    message += append;
                }

                return message;

            });
        }

        private string StrikeThrough()
        {
            switch (_formattingType)
            {
                case FormattingType.Slack:
                    return "~";
                case FormattingType.Discord:
                    return "~~";
                default:
                    return "ð";
            }
        }
    }
}
