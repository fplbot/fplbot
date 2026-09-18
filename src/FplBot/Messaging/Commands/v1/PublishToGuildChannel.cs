namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishToGuildChannel(string TeamId, string ChannelId, string Message);

public record PublishRichToGuildChannel(string TeamId, string ChannelId, string Title, string Description);

public record PublishSectionsToGuildChannel(string TeamId, string ChannelId, string Title, IReadOnlyList<RichSection> Sections);

public record RichSection(string? Heading, string Body);
