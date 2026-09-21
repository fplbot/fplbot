using StackExchange.Redis;

namespace FplBot.Data.Discord;

// Deliberately its own tiny repository, not a method on IGuildRepository/DiscordGuildRepository:
// this is a metric snapshot from Discord (approximate, includes bots), not part of the
// Installation aggregate's own state.
//
// Count/timestamp/community-flag are separate string keys rather than one hash: GuildMemberCount-{id}
// has been live in prod since the original single-count version, and HSET on a pre-existing string
// key throws WRONGTYPE - parallel keys need no migration. A guild counted before a given field
// existed just reads back its default (UnixEpoch / false) until its next successful sweep.
public class GuildMemberCountRepository(IConnectionMultiplexer redis) : IGuildMemberCountRepository
{
    private const string IndexKey = "GuildMemberCountIndex";

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SetApproximateMemberCount(string guildId, int approximateMemberCount, bool isCommunity = false)
    {
        await _db.StringSetAsync(CountKey(guildId), approximateMemberCount);
        await _db.StringSetAsync(UpdatedAtKey(guildId), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await _db.StringSetAsync(IsCommunityKey(guildId), isCommunity);
        await _db.SetAddAsync(IndexKey, guildId);
    }

    public async Task Delete(string guildId)
    {
        await _db.KeyDeleteAsync(CountKey(guildId));
        await _db.KeyDeleteAsync(UpdatedAtKey(guildId));
        await _db.KeyDeleteAsync(IsCommunityKey(guildId));
        await _db.SetRemoveAsync(IndexKey, guildId);
    }

    public async Task<IReadOnlyDictionary<string, GuildMemberCount>> GetAll()
    {
        var guildIds = await _db.SetMembersAsync(IndexKey);

        var batch = _db.CreateBatch();
        var countReads = guildIds.ToDictionary(g => g.ToString()!, g => batch.StringGetAsync(CountKey(g.ToString()!)));
        var updatedAtReads = guildIds.ToDictionary(g => g.ToString()!, g => batch.StringGetAsync(UpdatedAtKey(g.ToString()!)));
        var isCommunityReads = guildIds.ToDictionary(g => g.ToString()!, g => batch.StringGetAsync(IsCommunityKey(g.ToString()!)));
        batch.Execute();
        await Task.WhenAll(countReads.Values.Concat(updatedAtReads.Values).Concat(isCommunityReads.Values));

        return countReads
            .Where(r => r.Value.Result.HasValue)
            .ToDictionary(r => r.Key, r => new GuildMemberCount(
                (int)r.Value.Result,
                updatedAtReads[r.Key].Result.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds((long)updatedAtReads[r.Key].Result)
                    : DateTimeOffset.UnixEpoch,
                isCommunityReads[r.Key].Result.HasValue && (bool)isCommunityReads[r.Key].Result));
    }

    private static string CountKey(string guildId) => $"GuildMemberCount-{guildId}";
    private static string UpdatedAtKey(string guildId) => $"GuildMemberCountUpdatedAt-{guildId}";
    private static string IsCommunityKey(string guildId) => $"GuildMemberCountIsCommunity-{guildId}";
}
