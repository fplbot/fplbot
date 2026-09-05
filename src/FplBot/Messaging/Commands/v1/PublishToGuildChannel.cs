namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishToGuildChannel(string GuildId, string ChannelId, string Message);
public record PublishRichToGuildChannel(string GuildId, string ChannelId, string Title, string Description);
