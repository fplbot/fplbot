using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Fpl.Client;

internal class CacheProvider : ICacheProvider
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheProvider> _logger;

    public CacheProvider(IDistributedCache cache, ILogger<CacheProvider> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetCachedOrFetch<T>(string url, Func<string, Task<string>> jsonFetch, TimeSpan expireIn) where T: class
    {
        string cacheObj = await _cache.GetStringAsync(url);
        if (!string.IsNullOrEmpty(cacheObj))
        {
            _logger.LogInformation($"CACHE HIT: {url}");
            return JsonSerializer.Deserialize<T>(cacheObj, JsonConvert.JsonSerializerOptions);
        }
        _logger.LogInformation($"CACHE MISS: {url}");
        var json = await jsonFetch(url);
        if (!string.IsNullOrEmpty(json))
        {
            var result = JsonSerializer.Deserialize<T>(json, JsonConvert.JsonSerializerOptions);
            await _cache.SetStringAsync(url, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow =  expireIn });
            return result;
        }

        return null;
    }
}

public interface ICacheProvider
{
    Task<T> GetCachedOrFetch<T>(string url, Func<string, Task<string>> jsonFetch, TimeSpan expireIn) where T:class;
}