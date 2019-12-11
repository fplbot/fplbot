using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FplBot.ConsoleApps
{
    public class CookieFetcher
    {
        private readonly Authenticator _authenticator;
        private readonly CookieCache _cache;
        private readonly ILogger<CookieFetcher> _logger;

        public CookieFetcher(Authenticator authenticator, CookieCache cache, ILogger<CookieFetcher> logger)
        {
            _authenticator = authenticator;
            _cache = cache;
            _logger = logger;
        
        }
        
        public async Task<string> GetSessionCookie()
        {
            var cookieFromCache = await _cache.GetAsync();
            
            if (string.IsNullOrEmpty(cookieFromCache))
            {
                _logger.LogWarning("Cache miss. Re-authenticating.");
                var cookies = await _authenticator.Authenticate();
                
                var sessionCookieExpiry = cookies.First(c => c.Name == "sessionid").Expires;
                var cookieString = string.Join("; ", cookies);
                await _cache.SetAsync(cookieString, sessionCookieExpiry);
                return cookieString;
            }
            _logger.LogWarning("Cache hit");
            return cookieFromCache;
        }
    }
}