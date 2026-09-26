using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers;

public class DiscordChannelDeliveryFailedHandler(
    IGuildRepository repository,
    ILogger<DiscordChannelDeliveryFailedHandler> logger) : IConsumer<DiscordChannelDeliveryFailed>
{
    public async Task Consume(ConsumeContext<DiscordChannelDeliveryFailed> context)
    {
        var message = context.Message;
        var removed = await StaleChannelSubscriptions.RecordFailure(
            repository, message.TeamId, message.ChannelId, message.Reason, message.OccuredAt);

        if (removed is null)
        {
            return;
        }

        logger.LogWarning(
            "Removed stale subscription for guild {GuildId} channel {ChannelId}: {Reason} after {FailureCount} failures since {FailingSince}",
            message.TeamId, message.ChannelId, message.Reason, removed.FailureCount, removed.FailingSince);

        await context.Publish(new DiscordChannelSubscriptionRemoved(
            message.TeamId, message.ChannelId, message.Reason, removed.FailureCount, removed.FailingSince!.Value));

        var remaining = await repository.FindInstallationByTeamId(message.TeamId);
        if (remaining is { ChannelSubscriptions.Count: 0 })
        {
            await context.Publish(new PurgedLastSubscriptionForServer(message.TeamId));
        }
    }
}
