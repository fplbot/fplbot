using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record PlayerTransfersUpdated(int PlayerId, string PlayerName, decimal TransfersToOwnersRatio) : IEvent;
}