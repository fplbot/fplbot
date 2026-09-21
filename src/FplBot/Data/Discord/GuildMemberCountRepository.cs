using StackExchange.Redis;

namespace FplBot.Data.Discord;

// Deliberately its own tiny repository, not a method on IGuildRepository/DiscordGuildRepository:
// this is a metric snapshot from Discord (approximate, includes bots), not part of the
// Installation aggregate's own state.
public class GuildMemberCountRepository(IConnectionMultiplexer redis) : IGuildMemberCountRepository
{
    private const string IndexKey = "GuildMemberCountIndex";

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SetApproximateMemberCount(string guildId, int approximateMemberCount)
    {
        await _db.StringSetAsync(CountKey(guildId), approximateMemberCount);
        await _db.SetAddAsync(IndexKey, guildId);
    }

    public async Task Delete(string guildId)
    {
        await _db.KeyDeleteAsync(CountKey(guildId));
        await _db.SetRemoveAsync(IndexKey, guildId);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetAll()
    {
        var guildIds = await _db.SetMembersAsync(IndexKey);

        var batch = _db.CreateBatch();
        var reads = guildIds.ToDictionary(g => g.ToString()!, g => batch.StringGetAsync(CountKey(g.ToString()!)));
        batch.Execute();
        await Task.WhenAll(reads.Values);

        return reads
            .Where(r => r.Value.Result.HasValue)
            .ToDictionary(r => r.Key, r => (int)r.Value.Result);
    }

    private static string CountKey(string guildId) => $"GuildMemberCount-{guildId}";
}
