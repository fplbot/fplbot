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
            repository, message.GuildId, message.ChannelId, message.OccuredAt);

        if (removed is null)
        {
            return;
        }

        logger.LogWarning(
            "Removed stale subscription for guild {GuildId} channel {ChannelId}: {Reason} after {FailureCount} failures since {FailingSince}",
            message.GuildId, message.ChannelId, message.Reason, removed.FailureCount, removed.FailingSince);

        await context.Publish(new DiscordChannelSubscriptionRemoved(
            message.GuildId, message.ChannelId, message.Reason, removed.FailureCount, removed.FailingSince!.Value));
    }
}
