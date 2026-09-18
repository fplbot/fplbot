using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers;

public class ChannelMovedHandler(ILogger<ChannelMovedHandler> logger)
    : IConsumer<SlackChannelMoved>, IConsumer<DiscordChannelMoved>
{
    public Task Consume(ConsumeContext<SlackChannelMoved> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Slack team {TeamId} moved subscription from channel {OldChannelId} to {NewChannelId}",
            message.TeamId, message.OldChannelId, message.NewChannelId);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<DiscordChannelMoved> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Discord guild {GuildId} moved subscription from channel {OldChannelId} to {NewChannelId}",
            message.GuildId, message.OldChannelId, message.NewChannelId);
        return Task.CompletedTask;
    }
}
