namespace FplBot.Data.Discord;

public record GuildFplSubscription(string GuildId, string ChannelId, int? LeagueId, IEnumerable<EventSubscription> Subscriptions);
