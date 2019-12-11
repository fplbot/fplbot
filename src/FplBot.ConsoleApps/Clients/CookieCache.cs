using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace FplBot.ConsoleApps
{
    public class CookieCache
    {
        private readonly IDistributedCache _cache;
        private string AuthCookieCacheKey = "authcookie";

        public CookieCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<string> GetAsync()
        {
            var entry = await _cache.GetStringAsync(AuthCookieCacheKey);

            if (entry != null)
            {
                return entry;
            }

            return null;
        }

        public async Task SetAsync(string cookie, DateTime cookieExpiration)
        {
            DateTimeOffset expiration = DateTime.SpecifyKind(cookieExpiration, DateTimeKind.Utc);

            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiration
            };

            await _cache.SetStringAsync(AuthCookieCacheKey, cookie, entryOptions);
        }

        public Task DeleteAsync(string clientName)
        {
            if (clientName is null) throw new ArgumentNullException(nameof(clientName));

            return _cache.RemoveAsync(AuthCookieCacheKey);
        }
    }
}