using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;

namespace FplBot.Discord.Data
{
    public interface IGuildRepository : IGuildStore
    {
        Task<IEnumerable<Guild>> GetAllGuilds();
        Task<IEnumerable<GuildFplSubscription>> GetAllGuildSubscriptions();
        Task<GuildFplSubscription> GetGuildSubscription(string guildId, string channelId);
        Task DeleteGuildSubscription(string guildId, string channelId);
        Task UpdateGuildSubscription(GuildFplSubscription guildSub);
        Task InsertGuildSubscription(GuildFplSubscription guildSub);
        Task<IEnumerable<GuildFplSubscription>> GetAllSubscriptionInGuild(string guildId);


    }
}
