using StackExchange.Redis;

namespace FplBot.Data.Discord;

// Deliberately its own tiny repository, not a method on IGuildRepository/DiscordGuildRepository:
// this is a metric snapshot from Discord (approximate, includes bots), not part of the
// Installation aggregate's own state.
//
// Count and timestamp are separate string keys rather than one hash: GuildMemberCount-{id} has
// been live in prod since the original single-count version, and HSET on a pre-existing string
// key throws WRONGTYPE - two parallel keys need no migration. A guild counted before this field
// existed just reads back UpdatedAt as DateTimeOffset.UnixEpoch until its next successful sweep.
public class GuildMemberCountRepository(IConnectionMultiplexer redis) : IGuildMemberCountRepository
{
    private const string IndexKey = "GuildMemberCountIndex";

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SetApproximateMemberCount(string guildId, int approximateMemberCount)
    {
        await _db.StringSetAsync(CountKey(guildId), approximateMemberCount);
        await _db.StringSetAsync(UpdatedAtKey(guildId), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await _db.SetAddAsync(IndexKey, guildId);
    }

    public async Task Delete(string guildId)
    {
        await _db.KeyDeleteAsync(CountKey(guildId));
        await _db.KeyDeleteAsync(UpdatedAtKey(guildId));
        await _db.SetRemoveAsync(IndexKey, guildId);
    }

    public async Task<IReadOnlyDictionary<string, GuildMemberCount>> GetAll()
    {
        var guildIds = await _db.SetMembersAsync(IndexKey);

        var batch = _db.CreateBatch();
        var countReads = guildIds.ToDictionary(g => g.ToString()!, g => batch.StringGetAsync(CountKey(g.ToString()!)));
        var updatedAtReads = guildIds.ToDictionary(g => g.ToString()!, g => batch.StringGetAsync(UpdatedAtKey(g.ToString()!)));
        batch.Execute();
        await Task.WhenAll(countReads.Values.Concat(updatedAtReads.Values));

        return countReads
            .Where(r => r.Value.Result.HasValue)
            .ToDictionary(r => r.Key, r => new GuildMemberCount(
                (int)r.Value.Result,
                updatedAtReads[r.Key].Result.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds((long)updatedAtReads[r.Key].Result)
                    : DateTimeOffset.UnixEpoch));
    }

    private static string CountKey(string guildId) => $"GuildMemberCount-{guildId}";
    private static string UpdatedAtKey(string guildId) => $"GuildMemberCountUpdatedAt-{guildId}";
}
