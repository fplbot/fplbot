using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
{
    internal interface IFormatEvents
    {
        IEnumerable<string> Format(IEnumerable<PlayerEvent> events);
    }
}
