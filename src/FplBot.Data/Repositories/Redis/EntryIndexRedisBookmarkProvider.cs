using FplBot.Data.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace FplBot.Data.Repositories.Redis
{
    public class EntryIndexRedisBookmarkProvider : IEntryIndexBookmarkProvider
    {
        private readonly ILogger<EntryIndexRedisBookmarkProvider> _logger;
        private readonly IDatabase _db;
        private const string BookmarkKey = "entryIndexBookmark";

        public EntryIndexRedisBookmarkProvider(ConnectionMultiplexer redis, ILogger<EntryIndexRedisBookmarkProvider> logger)
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
