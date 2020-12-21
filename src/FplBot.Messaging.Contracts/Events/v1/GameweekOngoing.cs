using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record GameweekOngoing(int Id) : IEvent;
}