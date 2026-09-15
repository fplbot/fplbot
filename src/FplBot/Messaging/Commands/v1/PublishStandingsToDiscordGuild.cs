namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishStandingsToDiscordGuild(string GuildId, string ChannelId, int LeagueId, int GameweekId);
