using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record GameweekCurrentlyFinished(int Id) : IEvent;
}