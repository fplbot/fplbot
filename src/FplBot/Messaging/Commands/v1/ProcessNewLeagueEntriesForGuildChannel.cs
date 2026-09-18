namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessNewLeagueEntriesForGuildChannel(string TeamId, string ChannelId, int LeagueId, int GameweekId);
