using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record GameweekStarted(int Id) : IEvent;
}