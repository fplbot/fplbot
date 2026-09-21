using StackExchange.Redis;

namespace FplBot.Data.Slack;

// Deliberately its own tiny repository, not a method on ISlackTeamRepository/SlackTeamRepository:
// this is a metric snapshot from Slack (per-channel, since unlike a Discord guild a Slack
// channel's membership isn't implied by workspace membership - see conversations.info's
// num_members), not part of the Installation aggregate's own state. Summing across a
// workspace's channels double-counts a user in multiple subscribed channels, the same kind of
// approximation GuildMemberCountRepository already accepts for Discord.
public class ChannelMemberCountRepository(IConnectionMultiplexer redis) : IChannelMemberCountRepository
{
    private const string IndexKey = "SlackChannelMemberCountIndex";

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SetMemberCount(string channelId, int memberCount)
    {
        await _db.StringSetAsync(CountKey(channelId), memberCount);
        await _db.StringSetAsync(UpdatedAtKey(channelId), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await _db.SetAddAsync(IndexKey, channelId);
    }

    public async Task Delete(string channelId)
    {
        await _db.KeyDeleteAsync(CountKey(channelId));
        await _db.KeyDeleteAsync(UpdatedAtKey(channelId));
        await _db.SetRemoveAsync(IndexKey, channelId);
    }

    public async Task<IReadOnlyDictionary<string, ChannelMemberCount>> GetAll()
    {
        var channelIds = await _db.SetMembersAsync(IndexKey);

        var batch = _db.CreateBatch();
        var countReads = channelIds.ToDictionary(c => c.ToString()!, c => batch.StringGetAsync(CountKey(c.ToString()!)));
        var updatedAtReads = channelIds.ToDictionary(c => c.ToString()!, c => batch.StringGetAsync(UpdatedAtKey(c.ToString()!)));
        batch.Execute();
        await Task.WhenAll(countReads.Values.Concat(updatedAtReads.Values));

        return countReads
            .Where(r => r.Value.Result.HasValue)
            .ToDictionary(r => r.Key, r => new ChannelMemberCount(
                (int)r.Value.Result,
                updatedAtReads[r.Key].Result.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds((long)updatedAtReads[r.Key].Result)
                    : DateTimeOffset.UnixEpoch));
    }

    private static string CountKey(string channelId) => $"SlackChannelMemberCount-{channelId}";
    private static string UpdatedAtKey(string channelId) => $"SlackChannelMemberCountUpdatedAt-{channelId}";
}
