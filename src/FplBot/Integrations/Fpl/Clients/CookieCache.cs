using Microsoft.Extensions.Caching.Distributed;

namespace Fpl.Client.Clients;

public class CookieCache(IDistributedCache cache)
{
    private readonly string AuthCookieCacheKey = "authcookie";

    public async Task<string?> GetAsync()
    {
        return await cache.GetStringAsync(AuthCookieCacheKey);
    }

    public async Task SetAsync(string cookie, DateTime cookieExpiration)
    {
        DateTimeOffset expiration = DateTime.SpecifyKind(cookieExpiration, DateTimeKind.Utc);

        var entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiration
        };

        await cache.SetStringAsync(AuthCookieCacheKey, cookie, entryOptions);
    }
}
