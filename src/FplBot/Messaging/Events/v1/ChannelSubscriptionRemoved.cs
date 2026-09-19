namespace FplBot.Messaging.Contracts.Events.v1;

public record SlackChannelSubscriptionRemoved(string TeamId, string ChannelId, string Reason, int FailureCount, DateTimeOffset FailingSince);

public record DiscordChannelSubscriptionRemoved(string TeamId, string ChannelId, string Reason, int FailureCount, DateTimeOffset FailingSince);
