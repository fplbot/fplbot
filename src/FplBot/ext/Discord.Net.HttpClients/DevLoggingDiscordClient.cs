namespace Discord.Net.HttpClients;

public class DevLoggingDiscordClient : IDiscordClient
{
    private readonly DiscordClient _inner;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DevLoggingDiscordClient> _logger;

    public DevLoggingDiscordClient(DiscordClient inner, IHostEnvironment env, ILogger<DevLoggingDiscordClient> logger)
    {
        _inner = inner;
        _env = env;
        _logger = logger;
    }

    public async Task ChannelMessagePost(string channelId, string text)
    {
        if (!_env.IsDevelopment()) { await _inner.ChannelMessagePost(channelId, text); return; }
        _logger.LogInformation("[DEV] Discord → channel:{ChannelId}\n{Text}", channelId, text);
    }

    public async Task ChannelMessagePost(string channelId, DiscordClient.RichEmbed embed)
    {
        if (!_env.IsDevelopment()) { await _inner.ChannelMessagePost(channelId, embed); return; }
        _logger.LogInformation("[DEV] Discord → channel:{ChannelId} | {Title}\n{Description}", channelId, embed.Title, embed.Description);
    }
}
