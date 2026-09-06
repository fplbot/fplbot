namespace Discord.Net.HttpClients;

public class DevLoggingDiscordClient(DiscordClient inner, IHostEnvironment env, ILogger<DevLoggingDiscordClient> logger)
    : IDiscordClient
{
    public async Task ChannelMessagePost(string channelId, string text)
    {
        if (!env.IsDevelopment()) { await inner.ChannelMessagePost(channelId, text); return; }
        logger.LogInformation("[DEV] Discord → channel:{ChannelId}\n{Text}", channelId, text);
    }

    public async Task ChannelMessagePost(string channelId, DiscordClient.RichEmbed embed)
    {
        if (!env.IsDevelopment()) { await inner.ChannelMessagePost(channelId, embed); return; }
        logger.LogInformation("[DEV] Discord → channel:{ChannelId} | {Title}\n{Description}", channelId, embed.Title, embed.Description);
    }
}
