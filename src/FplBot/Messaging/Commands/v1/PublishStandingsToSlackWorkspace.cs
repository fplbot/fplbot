namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishStandingsToSlackWorkspace(string TeamId, string ChannelId, int LeagueId, int GameweekId);
