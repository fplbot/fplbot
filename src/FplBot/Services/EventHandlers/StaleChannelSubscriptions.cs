using FplBot.Data;
using FplBot.Domain;

namespace FplBot.EventHandlers;

public static class StaleChannelSubscriptions
{
    public static async Task<ChannelSubscription?> RecordFailure(
        IDomainRepository repository,
        string installationId,
        string channelId,
        DateTimeOffset now)
    {
        var subscription = await repository.GetChannelSubscription(installationId, channelId);
        if (subscription is null)
        {
            return null;
        }

        subscription.RecordDeliveryFailure(now);

        if (!subscription.IsStale(now))
        {
            await repository.SaveChannelSubscription(installationId, subscription);
            return null;
        }

        await repository.DeleteChannelSubscription(installationId, channelId);
        return subscription;
    }
}
