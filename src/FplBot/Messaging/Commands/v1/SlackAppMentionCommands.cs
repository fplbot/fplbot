namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessSubscribeCommand(string TeamId, string Channel, string Text);

public record ProcessSubscriptionsCommand(string TeamId, string Channel);

public record ProcessFollowLeagueCommand(string TeamId, string Channel, string Text);

public record ProcessStandingsCommand(string TeamId, string Channel);

public record ProcessCaptainsCommand(string TeamId, string Channel, string Text);

public record ProcessTransfersCommand(string TeamId, string Channel, string Text);

public record ProcessInjuriesCommand(string TeamId, string Channel);

public record ProcessNextGameweekCommand(string TeamId, string Channel, string User);

public record ProcessPlayerCommand(string TeamId, string Channel, string Text);

public record ProcessPriceChangesCommand(string TeamId, string Channel);

public record ProcessSearchCommand(string TeamId, string Channel, string User, string Text);

public record ProcessDebugCommand(string TeamId, string Channel);

public record ProcessHelpCommand(string TeamId, string Channel);

public record ProcessUnknownAppMention(string TeamId, string Channel, string User, string Ts, string Text);

public record ProcessBotJoinedChannel(string TeamId, string Channel, string User);
