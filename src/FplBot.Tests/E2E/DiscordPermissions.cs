namespace FplBot.Tests.E2E;

public static class DiscordPermissions
{
    public const long ViewChannel = 1L << 10;
    public const long SendMessages = 1L << 11;
    public const long EmbedLinks = 1L << 14;

    public const long All = ViewChannel | SendMessages | EmbedLinks;
    public const long PlainTextOnly = ViewChannel | SendMessages;
    public const long None = ViewChannel;
    public const long Unknown = 0;
}
