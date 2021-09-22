using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record FixtureFinished(int FixtureId) : IEvent;
}
