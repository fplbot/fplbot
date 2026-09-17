using StackExchange.Redis;

namespace FplBot.Data.Leagues;

public class LeagueEntriesRedisBookmarkProvider(
    IConnectionMultiplexer redis,
    ILogger<LeagueEntriesRedisBookmarkProvider> logger)
    : ILeagueEntriesBookmarkProvider
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<DateTime?> GetLastSeenJoinTime(int leagueId)
    {
        var stored = await _db.StringGetAsync(ToKey(leagueId));
        if (!stored.HasValue)
        {
            return null;
        }

        if (!stored.TryParse(out long unixTimeMilliseconds))
        {
            logger.LogWarning("Unable to parse last seen join time '{stored}' for league {leagueId}", stored.ToString(), leagueId);
            return null;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds).UtcDateTime;
    }

    public async Task SetLastSeenJoinTime(int leagueId, DateTime joinTime)
    {
        var asUtc = joinTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(joinTime, DateTimeKind.Utc)
            : joinTime.ToUniversalTime();

        var success = await _db.StringSetAsync(ToKey(leagueId), new DateTimeOffset(asUtc).ToUnixTimeMilliseconds());
        if (!success)
        {
            logger.LogError("Unable to store last seen join time for league {leagueId}", leagueId);
        }
    }

    private static string ToKey(int leagueId) => $"LeagueEntriesBookmark-{leagueId}";
}
