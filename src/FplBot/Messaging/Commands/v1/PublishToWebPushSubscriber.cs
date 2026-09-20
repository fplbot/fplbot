namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishToWebPushSubscriber(string SubscriberId, string Title, string Body, int? LeagueId);

public record BroadcastToWebPush(string Title, string Body);
