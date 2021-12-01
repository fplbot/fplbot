using System.Net;
using Discord.Net.HttpClients;
using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Discord.Handlers;

public class PublishToGuildHandler :
    IHandleMessages<PublishToGuildChannel>,
    IHandleMessages<PublishRichToGuildChannel>
{
    private readonly DiscordClient _discordClient;
    private readonly ILogger<PublishToGuildHandler> _logger;

    public PublishToGuildHandler(DiscordClient discordClient, ILogger<PublishToGuildHandler> logger)
    {
        _discordClient = discordClient;
        _logger = logger;
    }

    public async Task Handle(PublishToGuildChannel message, IMessageHandlerContext context)
    {
        await _discordClient.ChannelMessagePost(message.ChannelId, message.Message);
    }

    public async Task Handle(PublishRichToGuildChannel message, IMessageHandlerContext context)
    {
        try
        {
            await _discordClient.ChannelMessagePost(message.ChannelId, new DiscordClient.RichEmbed(message.Title, message.Description));
        }
        catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.Forbidden)
        {
            // Scenarios:
            // - Setup a subscription in a channel without giving the bot permissions (fplbot role needs access)
            _logger.LogWarning("Unauthorized to post to Discord channel {channel}", message.ChannelId);
        }
    }
}
