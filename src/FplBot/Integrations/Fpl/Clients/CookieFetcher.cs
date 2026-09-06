namespace Fpl.Client.Clients;

public class CookieFetcher(Authenticator authenticator, CookieCache cache, ILogger<CookieFetcher> logger)
{
    public async Task<string> GetSessionCookie()
    {
        var cookieFromCache = await cache.GetAsync();
            
        if (string.IsNullOrEmpty(cookieFromCache))
        {
            logger.LogInformation("Cache miss. Re-authenticating.");
            var cookies = await authenticator.Authenticate();
                
            var sessionCookieExpiry = cookies.First(c => c.Name == "sessionid").Expires;
            var cookieString = string.Join("; ", cookies);
            await cache.SetAsync(cookieString, sessionCookieExpiry);
            return cookieString;
        }
        logger.LogDebug("Cache hit");
        return cookieFromCache;
    }
}
