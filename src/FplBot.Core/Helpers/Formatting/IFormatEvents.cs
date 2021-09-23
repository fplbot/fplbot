using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
{
    internal interface IFormatEvents : IFormat
    {
        string EventDescriptionSingular { get; }
        string EventDescriptionPlural { get; }

        string EventEmoji { get; }
    }

    internal interface IFormat
    {
        IEnumerable<string> Format(IEnumerable<PlayerEvent> events);
    }
}
