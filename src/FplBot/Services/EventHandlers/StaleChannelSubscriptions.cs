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
        var installation = await repository.FindInstallationByTeamId(installationId);
        var subscription = installation?.GetChannel(channelId);
        if (installation is null || subscription is null)
        {
            return null;
        }

        subscription.RecordDeliveryFailure(now);

        if (subscription.IsStale(now))
        {
            installation.RemoveChannel(channelId);
            await repository.Save(installation);
            return subscription;
        }

        await repository.Save(installation);
        return null;
    }
}
