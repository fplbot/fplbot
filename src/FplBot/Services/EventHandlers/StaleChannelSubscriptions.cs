using FplBot.Data;
using FplBot.Domain;

namespace FplBot.EventHandlers;

public static class StaleChannelSubscriptions
{
    public static async Task<ChannelSubscription?> RecordFailure(
        IDomainRepository repository,
        string installationId,
        string channelId,
        string reason,
        DateTimeOffset now)
    {
        var subscription = await repository.GetChannelSubscription(installationId, channelId);
        if (subscription is null)
        {
            return null;
        }

        subscription.RecordDeliveryFailure(now, reason);

        if (!subscription.IsStale(now))
        {
            await repository.SaveChannelSubscription(installationId, subscription);
            return null;
        }

        await repository.DeleteChannelSubscription(installationId, channelId);
        return subscription;
    }

    public static async Task ClearFailures(
        IDomainRepository repository,
        string installationId,
        string channelId,
        ILogger logger)
    {
        try
        {
            var subscription = await repository.GetChannelSubscription(installationId, channelId);
            if (subscription is null || subscription.FailureCount == 0)
            {
                return;
            }

            subscription.ClearDeliveryFailures();
            await repository.SaveChannelSubscription(installationId, subscription);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Could not clear delivery failures for {InstallationId} channel {ChannelId}", installationId, channelId);
        }
    }
}
