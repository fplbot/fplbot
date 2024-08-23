namespace FplBot.Data.Discord;

public interface IGuildRepository
{
    Task<IEnumerable<GuildRepoGuild>> GetAllGuilds();
    Task<IEnumerable<GuildFplSubscription>> GetAllGuildSubscriptions();
    Task<GuildFplSubscription> GetGuildSubscription(string guildId, string channelId);
    Task DeleteGuildSubscription(string guildId, string channelId);
    Task UpdateGuildSubscription(GuildFplSubscription guildSub);
    Task InsertGuildSubscription(GuildFplSubscription guildSub);
    Task DeleteGuild(string guildId);
}

public record GuildRepoGuild(string Id, string Name);
