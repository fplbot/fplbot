using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Fpl.Client;

internal class CacheProvider(IDistributedCache cache, ILogger<CacheProvider> logger) : ICacheProvider
{
    public async Task<T?> GetCachedOrFetch<T>(string url, Func<string, Task<string>> jsonFetch, TimeSpan expireIn) where T: class
    {
        var cacheObj = await cache.GetStringAsync(url);
        if (!string.IsNullOrEmpty(cacheObj))
        {
            logger.LogInformation($"CACHE HIT: {url}");
            return JsonSerializer.Deserialize<T>(cacheObj, JsonConvert.JsonSerializerOptions);
        }
        logger.LogInformation($"CACHE MISS: {url}");
        var json = await jsonFetch(url);
        if (!string.IsNullOrEmpty(json))
        {
            var result = JsonSerializer.Deserialize<T>(json, JsonConvert.JsonSerializerOptions);
            await cache.SetStringAsync(url, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow =  expireIn });
            return result;
        }

        return null;
    }
}

public interface ICacheProvider
{
    Task<T?> GetCachedOrFetch<T>(string url, Func<string, Task<string>> jsonFetch, TimeSpan expireIn) where T:class;
}
