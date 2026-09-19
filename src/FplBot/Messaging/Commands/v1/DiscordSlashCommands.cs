namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessFollowCommand(string TeamId, string ChannelId, string InteractionToken, int LeagueId, long AppPermissions);

public record ProcessAddSubscriptionCommand(string TeamId, string ChannelId, string InteractionToken, string Subscription, long AppPermissions);

public record ProcessRemoveSubscriptionCommand(string TeamId, string ChannelId, string InteractionToken, string Subscription, long AppPermissions);

public record ProcessDiscordHelpCommand(string TeamId, string ChannelId, string InteractionToken);
