namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishStandingsToDiscordGuild(string TeamId, string ChannelId, int LeagueId, int GameweekId);
