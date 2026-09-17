using Discord.Net.HttpClients.Components;
using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.EventHandlers.Discord;

public static class DiscordCards
{
    private const int FplPurple = 0x37003C;

    public static ComponentRequest HeadingCard(string heading, string body) =>
        Card([new TextDisplay($"### {heading}"), new TextDisplay(body)]);

    public static ComponentRequest SectionedCard(string title, IReadOnlyList<RichSection> sections)
    {
        List<MessageComponent> components = [new TextDisplay($"## {title}")];
        foreach (var section in sections)
        {
            components.Add(new Separator());
            if (!string.IsNullOrWhiteSpace(section.Heading))
            {
                components.Add(new TextDisplay($"### {section.Heading}"));
            }

            components.Add(new TextDisplay(section.Body));
        }

        return Card(components);
    }

    private static ComponentRequest Card(IReadOnlyList<MessageComponent> components) =>
        new([new Container(components, FplPurple)]);
}
