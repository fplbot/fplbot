using System.Threading.Tasks;
using Fpl.Search.Data.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Fpl.Search.Data.Repositories
{
    public class LeagueIndexRedisBookmarkProvider : ILeagueIndexBookmarkProvider
    {
        private readonly ILogger<LeagueIndexRedisBookmarkProvider> _logger;
        private readonly IDatabase _db;
        private const string BookmarkKey = "leagueIndexBookmark";

        public LeagueIndexRedisBookmarkProvider(IConnectionMultiplexer redis, ILogger<LeagueIndexRedisBookmarkProvider> logger)
        {
            _logger = logger;
            _db = redis.GetDatabase();
        }

        public async Task<int> GetBookmark()
        {
            var valid = (await _db.StringGetAsync(BookmarkKey)).TryParse(out int bookmark);

            if(!valid)
                _logger.LogWarning($"Unable to parse {BookmarkKey} from db");

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
