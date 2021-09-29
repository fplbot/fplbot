
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Fpl.Client
{
    internal class CacheProvider : ICacheProvider
    {
        private readonly IDistributedCache _cache;

        public CacheProvider(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetCachedOrFetch<T>(string url, Func<string, Task<string>> jsonFetch, TimeSpan expireIn) where T: class
        {
            string cacheObj = await _cache.GetStringAsync(url);
            if (!string.IsNullOrEmpty(cacheObj))
            {
                return JsonConvert.DeserializeObject<T>(cacheObj);
            }

            var json = await jsonFetch(url);
            if (!string.IsNullOrEmpty(json))
            {
                var result = JsonConvert.DeserializeObject<T>(json);
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
}
