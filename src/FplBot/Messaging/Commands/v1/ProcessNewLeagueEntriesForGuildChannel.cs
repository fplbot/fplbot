namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessNewLeagueEntriesForGuildChannel(string GuildId, string ChannelId, int LeagueId, int GameweekId);
