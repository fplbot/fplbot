using Discord.Net.HttpClients;
using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class PublishToGuildHandler(
    IDiscordClient discordClient,
    IGuildRepository guildRepository,
    ILogger<PublishToGuildHandler> logger,
    IHostEnvironment env)
    :
        IConsumer<PublishToGuildChannel>,
        IConsumer<PublishRichToGuildChannel>
{
    public async Task Consume(ConsumeContext<PublishToGuildChannel> context)
    {
        var message = context.Message;
        var publishMessage = message.Message;
        if (env.IsDevelopment())
        {
            publishMessage = $"[{Environment.MachineName}]\n{publishMessage}";
        }

        await Post(context, message.GuildId, message.ChannelId,
            () => discordClient.ChannelMessagePost(message.ChannelId, publishMessage));
    }

    public async Task Consume(ConsumeContext<PublishRichToGuildChannel> context)
    {
        var message = context.Message;
        int? color = null;
        if (env.IsDevelopment())
        {
            color = 14177041;
        }

        await Post(context, message.GuildId, message.ChannelId,
            () => discordClient.ChannelMessagePost(message.ChannelId,
                new DiscordClient.RichEmbed(message.Title, message.Description, color)));
    }

    private async Task Post(ConsumeContext context, string guildId, string channelId, Func<Task> post)
    {
        try
        {
            await post();
            await ClearFailures(guildId, channelId);
        }
        catch (DiscordApiException e)
        {
            if (DeliveryFailureClassifier.Classify(e) is { } reason)
            {
                logger.LogWarning(e, "Delivery to Discord channel {ChannelId} failed: {Reason}", channelId, reason);
                await context.Publish(new DiscordChannelDeliveryFailed(guildId, channelId, reason, DateTimeOffset.UtcNow));
            }
            else
            {
                logger.LogWarning(e, "Delivery to Discord channel {ChannelId} failed, not counted", channelId);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Delivery to Discord channel {ChannelId} failed, not counted", channelId);
        }
    }

    private async Task ClearFailures(string guildId, string channelId)
    {
        var subscription = await guildRepository.GetChannelSubscription(guildId, channelId);
        if (subscription is null || subscription.FailureCount == 0)
        {
            return;
        }

        subscription.ClearDeliveryFailures();
        await guildRepository.SaveChannelSubscription(guildId, subscription);
    }
}
