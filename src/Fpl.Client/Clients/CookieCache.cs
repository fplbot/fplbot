using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Fpl.Client.Clients
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
            return await _cache.GetStringAsync(AuthCookieCacheKey);
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
    }
}