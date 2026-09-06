using System.Net;
using Discord.Net.HttpClients;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class PublishToGuildHandler(
    IDiscordClient discordClient,
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
        await discordClient.ChannelMessagePost(message.ChannelId, publishMessage);
    }

    public async Task Consume(ConsumeContext<PublishRichToGuildChannel> context)
    {
        var message = context.Message;
        int? color = null;

        if (env.IsDevelopment())
        {
            color = 14177041;
        }

        try
        {
            await discordClient.ChannelMessagePost(message.ChannelId, new DiscordClient.RichEmbed(message.Title, message.Description, color));
        }
        catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.Forbidden)
        {
            // Scenarios:
            // - Setup a subscription in a channel without giving the bot permissions (fplbot role needs access)
            logger.LogWarning("Unauthorized to post to Discord channel {channel}", message.ChannelId);
        }
        catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.NotFound)
        {
            // Scenarios:
            // - Deleted channel?
            logger.LogWarning("Discord channel {channel} not found", message.ChannelId);
        }
    }
}
