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
        IConsumer<PublishRichToGuildChannel>,
        IConsumer<PublishSectionsToGuildChannel>,
        IConsumer<RespondToDiscordInteraction>
{
    public async Task Consume(ConsumeContext<RespondToDiscordInteraction> context)
    {
        var message = context.Message;

        await discordClient.InteractionFollowupPost(message.InteractionToken,
            DiscordCards.HeadingCard(message.Title, message.Description));
    }

    public async Task Consume(ConsumeContext<PublishToGuildChannel> context)
    {
        var message = context.Message;
        var publishMessage = message.Message;
        if (env.IsLocal())
        {
            publishMessage = $"[{Environment.MachineName}]\n{publishMessage}";
        }

        await Post(context, message.TeamId, message.ChannelId,
            () => discordClient.ChannelMessagePost(message.ChannelId, publishMessage));
    }

    public async Task Consume(ConsumeContext<PublishRichToGuildChannel> context)
    {
        var message = context.Message;

        await Post(context, message.TeamId, message.ChannelId,
            () => discordClient.ChannelMessagePost(message.ChannelId,
                DiscordCards.HeadingCard(message.Title, message.Description)));
    }

    public async Task Consume(ConsumeContext<PublishSectionsToGuildChannel> context)
    {
        var message = context.Message;

        await Post(context, message.TeamId, message.ChannelId,
            () => discordClient.ChannelMessagePost(message.ChannelId,
                DiscordCards.SectionedCard(message.Title, message.Sections)));
    }

    private async Task Post(ConsumeContext context, string guildId, string channelId, Func<Task> post)
    {
        try
        {
            await post();
            await StaleChannelSubscriptions.ClearFailures(guildRepository, guildId, channelId, logger);
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
}
