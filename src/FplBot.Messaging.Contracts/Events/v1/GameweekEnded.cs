using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record GameweekEnded(int Id) : IEvent;
}