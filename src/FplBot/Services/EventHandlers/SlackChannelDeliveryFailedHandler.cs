using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers;

public class SlackChannelDeliveryFailedHandler(
    ISlackTeamRepository repository,
    ILogger<SlackChannelDeliveryFailedHandler> logger) : IConsumer<SlackChannelDeliveryFailed>
{
    public async Task Consume(ConsumeContext<SlackChannelDeliveryFailed> context)
    {
        var message = context.Message;
        var removed = await StaleChannelSubscriptions.RecordFailure(
            repository, message.TeamId, message.ChannelId, message.OccuredAt);

        if (removed is null)
        {
            return;
        }

        logger.LogWarning(
            "Removed stale subscription for team {TeamId} channel {ChannelId}: {Reason} after {FailureCount} failures since {FailingSince}",
            message.TeamId, message.ChannelId, message.Reason, removed.FailureCount, removed.FailingSince);

        await context.Publish(new SlackChannelSubscriptionRemoved(
            message.TeamId, message.ChannelId, message.Reason, removed.FailureCount, removed.FailingSince!.Value));
    }
}
