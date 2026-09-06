using System.Net;
using Discord.Net.HttpClients;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace FplBot.EventHandlers.Discord;

public class PublishToGuildHandler :
    IConsumer<PublishToGuildChannel>,
    IConsumer<PublishRichToGuildChannel>
{
    private readonly IDiscordClient _discordClient;
    private readonly ILogger<PublishToGuildHandler> _logger;
    private readonly IHostEnvironment _env;

    public PublishToGuildHandler(IDiscordClient discordClient, ILogger<PublishToGuildHandler> logger, IHostEnvironment env)
    {
        _discordClient = discordClient;
        _logger = logger;
        _env = env;
    }

    public async Task Consume(ConsumeContext<PublishToGuildChannel> context)
    {
        var message = context.Message;
        var publishMessage = message.Message;
        if (_env.IsDevelopment())
        {
            publishMessage = $"[{Environment.MachineName}]\n{publishMessage}";
        }
        await _discordClient.ChannelMessagePost(message.ChannelId, publishMessage);
    }

    public async Task Consume(ConsumeContext<PublishRichToGuildChannel> context)
    {
        var message = context.Message;
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
