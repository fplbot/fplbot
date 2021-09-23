using System.Collections.Generic;
using System.Linq;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Taunts
{
    internal class TauntyFormatter
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
            return goalEvents.Select(g =>
            {
                var message = $"{g.Player.FirstName} {g.Player.SecondName} {_formatter.EventDescription} {_formatter.EventEmoji} ";

                if (g.IsRemoved)
                {
                    message = $"~{message.TrimEnd()}~ (VAR? :shrug:)";
                }
                else
                {
                    var tauntibleEntries = _tauntData.GetTauntibleEntries(g.Player, _formatter.Type);
                    var append = tauntibleEntries.Any() ? $" {string.Format(_formatter.JokePool.GetRandom(), string.Join(", ", tauntibleEntries))}" : null;
                    message += append;
                }

                return message;

            }).WhereNotNull().ToList();
        }
    }
}
