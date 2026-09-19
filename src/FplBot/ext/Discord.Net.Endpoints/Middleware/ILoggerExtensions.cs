namespace Discord.Net.Endpoints.Middleware;

internal static class ILoggerExtensions
{
    public static IDisposable? AddInteractionContext(this ILogger logger, string? guildId, string? channelId)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            [DiscordDiagnostics.GuildIdTag] = guildId ?? string.Empty,
            [DiscordDiagnostics.ChannelIdTag] = channelId ?? string.Empty
        });
    }
}
