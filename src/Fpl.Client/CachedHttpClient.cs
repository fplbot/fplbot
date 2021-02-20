using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class CachedHttpClient : ICachedHttpClient
    {
        private readonly HttpClient _client;
        private readonly IMemoryCache _cache;

        public CachedHttpClient(HttpClient client, IMemoryCache cache)
        {
            _client = client;
            _cache = cache;
        }

        public async Task<T> GetCachedOrFetch<T>(string url, TimeSpan expireIn)
        {
            if (_cache.TryGetValue(url, out T result))
            {
                return result;
            }

            var json = await _client.GetStringAsync(url);
            result = JsonConvert.DeserializeObject<T>(json);

            _cache.Set(url, result, expireIn);

            return result;
        }
    }

    public interface ICachedHttpClient
    {
        Task<T> GetCachedOrFetch<T>(string url, TimeSpan expireIn);
    }
}