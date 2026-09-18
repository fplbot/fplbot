namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessNewLeagueEntriesForSlackChannel(string TeamId, string ChannelId, int LeagueId, int GameweekId);
