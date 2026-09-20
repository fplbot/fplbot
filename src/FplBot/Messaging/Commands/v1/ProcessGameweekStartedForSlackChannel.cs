namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessGameweekStartedForSlackChannel(string TeamId, string ChannelId, int GameweekId);
