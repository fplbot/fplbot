using Discord.Net.HttpClients.Components;

namespace FplBot.EventHandlers.Discord;

public static class DiscordCards
{
    private const int DefaultAccentColor = 3604540;

    public static ComponentRequest HeadingCard(string heading, string body, int? accentColor) =>
        new([new Container([new TextDisplay($"# {heading}"), new TextDisplay(body)], accentColor ?? DefaultAccentColor)]);
}
