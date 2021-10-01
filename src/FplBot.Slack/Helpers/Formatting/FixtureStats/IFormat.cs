using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers.Formatting.FixtureStats
{
    internal interface IFormat
    {
        IEnumerable<string> Format(IEnumerable<PlayerEvent> events);
    }
}
