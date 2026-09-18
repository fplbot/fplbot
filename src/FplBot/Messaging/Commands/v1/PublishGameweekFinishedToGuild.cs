namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishGameweekFinishedToGuild(string TeamId, string ChannelId, int? LeagueId, int GameweekId);
