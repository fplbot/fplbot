using System.Net;
using Discord.Net.HttpClients;
using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class PublishToGuildHandler :
    IHandleMessages<PublishToGuildChannel>,
    IHandleMessages<PublishRichToGuildChannel>
{
    private readonly DiscordClient _discordClient;
    private readonly ILogger<PublishToGuildHandler> _logger;
    private readonly IHostEnvironment _env;

    public PublishToGuildHandler(DiscordClient discordClient, ILogger<PublishToGuildHandler> logger, IHostEnvironment env)
    {
        _discordClient = discordClient;
        _logger = logger;
        _env = env;
    }

    public async Task Handle(PublishToGuildChannel message, IMessageHandlerContext context)
    {
        var publishMessage = message.Message;
        if (_env.IsDevelopment())
        {
            publishMessage = $"[{Environment.MachineName}]\n{publishMessage}";
        }
        await _discordClient.ChannelMessagePost(message.ChannelId, publishMessage);
    }

    public async Task Handle(PublishRichToGuildChannel message, IMessageHandlerContext context)
    {
        int? color = null;

        if (_env.IsDevelopment())
        {
            color = 14177041;
        }

        try
        {
            await _discordClient.ChannelMessagePost(message.ChannelId, new DiscordClient.RichEmbed(message.Title, message.Description, color));
        }
        catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.Forbidden)
        {
            // Scenarios:
            // - Setup a subscription in a channel without giving the bot permissions (fplbot role needs access)
            _logger.LogWarning("Unauthorized to post to Discord channel {channel}", message.ChannelId);
        }
        catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.NotFound)
        {
            // Scenarios:
            // - Deleted channel?
            _logger.LogWarning("Discord channel {channel} not found", message.ChannelId);
        }
    }
}
