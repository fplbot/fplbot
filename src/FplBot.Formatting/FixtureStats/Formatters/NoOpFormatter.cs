using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats.Formatters;

internal class NoOpFormatter : IFormat
{
    public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
    {
        return Enumerable.Empty<string>();
    }
}