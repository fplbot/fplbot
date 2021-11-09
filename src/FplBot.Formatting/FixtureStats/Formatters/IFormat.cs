using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats.Formatters;

internal interface IFormat
{
    IEnumerable<string> Format(IEnumerable<PlayerEvent> events);
}