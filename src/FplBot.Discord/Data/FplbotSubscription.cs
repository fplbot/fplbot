namespace FplBot.Discord.Data;

public record GuildFplSubscription(string GuildId, string ChannelId, int? LeagueId, IEnumerable<EventSubscription> Subscriptions);
