namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessNewLeagueEntriesForSlackChannel(string WorkspaceId, string ChannelId, int LeagueId, int GameweekId);
