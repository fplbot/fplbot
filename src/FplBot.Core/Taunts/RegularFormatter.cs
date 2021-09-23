using System.Collections.Generic;
using System.Linq;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Taunts
{
    internal class RegularFormatter
    {
        private readonly IFormatWithTaunts _formatter;

        public RegularFormatter(IFormatWithTaunts formatter)
        {
            _formatter = formatter;
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
                return message;

            }).WhereNotNull().ToList();
        }
    }
}
