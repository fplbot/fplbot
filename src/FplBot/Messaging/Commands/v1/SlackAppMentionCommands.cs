namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessSubscribeCommand(string TeamId, string ChannelId, string Text);

public record ProcessSubscriptionsCommand(string TeamId, string ChannelId);

public record ProcessFollowLeagueCommand(string TeamId, string ChannelId, string Text);

public record ProcessStandingsCommand(string TeamId, string ChannelId);

public record ProcessCaptainsCommand(string TeamId, string ChannelId, string Text);

public record ProcessTransfersCommand(string TeamId, string ChannelId, string Text);

public record ProcessInjuriesCommand(string TeamId, string ChannelId);

public record ProcessNextGameweekCommand(string TeamId, string ChannelId, string User);

public record ProcessPlayerCommand(string TeamId, string ChannelId, string Text);

public record ProcessPriceChangesCommand(string TeamId, string ChannelId);

public record ProcessSearchCommand(string TeamId, string ChannelId, string User, string Text);

public record ProcessDebugCommand(string TeamId, string ChannelId);

public record ProcessHelpCommand(string TeamId, string ChannelId);

public record ProcessUnknownAppMention(string TeamId, string ChannelId, string User, string Ts, string Text);

public record ProcessBotJoinedChannel(string TeamId, string ChannelId, string User);
