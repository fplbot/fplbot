using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Fpl.Search.Indexing
{
    public class LeagueIndexRedisBookmarkProvider : IIndexBookmarkProvider
    {
        private readonly ILogger<LeagueIndexRedisBookmarkProvider> _logger;
        private readonly IDatabase _db;
        private const string BookmarkKey = "leagueIndexBookmark";

        public LeagueIndexRedisBookmarkProvider(ConnectionMultiplexer redis, ILogger<LeagueIndexRedisBookmarkProvider> logger)
        {
            _logger = logger;
            _db = redis.GetDatabase();
        }

        public async Task<int> GetBookmark()
        {
            var valid = (await _db.StringGetAsync(BookmarkKey)).TryParse(out int bookmark);
            _logger.LogError($"Unable to parse {BookmarkKey} from db");

            return valid ? bookmark : 1;
        }

        public async Task SetBookmark(int bookmark)
        {
            var success = await _db.StringSetAsync(BookmarkKey, bookmark);
            if (!success)
            {
                _logger.LogError($"Unable to set {BookmarkKey} in db");
            }
        }
    }
}