using System;
using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
{
    internal class NoOpFormatter : IFormatEvents
    {
        public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
        {
            return Array.Empty<string>();
        }
    }
}
