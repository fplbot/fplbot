namespace FplBot.Messaging.Contracts.Events.v1;

public record SlackChannelDeliveryFailed(string TeamId, string ChannelId, string Reason, DateTimeOffset OccuredAt);

public record DiscordChannelDeliveryFailed(string GuildId, string ChannelId, string Reason, DateTimeOffset OccuredAt);
