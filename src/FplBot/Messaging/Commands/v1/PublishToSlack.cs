namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishToSlack(string TeamId, string Channel, string Message);

public record PublishSlackThreadMessage(string TeamId, string Channel, string Timestamp, string Message);
