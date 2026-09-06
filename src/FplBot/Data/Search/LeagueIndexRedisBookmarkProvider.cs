using Fpl.Search.Data.Abstractions;
using StackExchange.Redis;

namespace Fpl.Search.Data.Repositories;

public class LeagueIndexRedisBookmarkProvider(
    IConnectionMultiplexer redis,
    ILogger<LeagueIndexRedisBookmarkProvider> logger)
    : ILeagueIndexBookmarkProvider
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string BookmarkKey = "leagueIndexBookmark";

    public async Task<int> GetBookmark()
    {
        var valid = (await _db.StringGetAsync(BookmarkKey)).TryParse(out int bookmark);

        if(!valid)
            logger.LogWarning($"Unable to parse {BookmarkKey} from db");

        return valid ? bookmark : 1;
    }

    public async Task SetBookmark(int bookmark)
    {
        var success = await _db.StringSetAsync(BookmarkKey, bookmark);
        if (!success)
        {
            logger.LogError($"Unable to set {BookmarkKey} in db");
        }
    }
}
