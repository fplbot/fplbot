namespace FplBot.Messaging.Contracts.Events.v1;

public record SlackChannelMoved(string TeamId, string OldChannelId, string NewChannelId);

public record DiscordChannelMoved(string TeamId, string OldChannelId, string NewChannelId);
