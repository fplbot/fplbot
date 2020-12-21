using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record PlayerTrending(int PlayerId, string PlayerName, int TrendCount, decimal TransfersToOwnersRatio) : IEvent;
}