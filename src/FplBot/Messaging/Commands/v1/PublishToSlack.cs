namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishToSlack(string TeamId, string ChannelId, string Message);

public record PublishSlackThreadMessage(string TeamId, string ChannelId, string Timestamp, string Message);
