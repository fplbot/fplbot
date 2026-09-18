namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessFollowCommand(string GuildId, string ChannelId, string InteractionToken, int LeagueId, long AppPermissions);

public record ProcessAddSubscriptionCommand(string GuildId, string ChannelId, string InteractionToken, string Subscription, long AppPermissions);

public record ProcessRemoveSubscriptionCommand(string GuildId, string ChannelId, string InteractionToken, string Subscription, long AppPermissions);

public record ProcessDiscordHelpCommand(string GuildId, string ChannelId, string InteractionToken);
