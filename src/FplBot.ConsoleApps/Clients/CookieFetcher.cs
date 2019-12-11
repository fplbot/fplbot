using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FplBot.ConsoleApps;
using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FplBot.ConsoleApps
{
    public class CookieFetcher
    {
        private readonly CookieCache _cache;
        private readonly ILogger<CookieFetcher> _logger;
        private readonly FplApiClientOptions _options;

        public CookieFetcher(IOptions<FplApiClientOptions> options, CookieCache cache, ILogger<CookieFetcher> logger)
        {
            _cache = cache;
            _logger = logger;
            _options = options.Value;
            _options.Validate(); 
        }
        
        public async Task<string> GetSessionCookie()
        {
            var cookieFromCache = await _cache.GetAsync();
            
            if (string.IsNullOrEmpty(cookieFromCache))
            {
                _logger.LogWarning("Cache miss. Re-authenticating.");
                var cookies = await Authenticate();
                
                var sessionCookieExpiry = cookies.First(c => c.Name == "sessionid").Expires;
                var cookieString = string.Join("; ", cookies);
                await _cache.SetAsync(cookieString, sessionCookieExpiry);
                return cookieString;
            }
            _logger.LogWarning("Cache hit");
            return cookieFromCache;
        }

        private async Task<List<Cookie>> Authenticate()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://users.premierleague.com/accounts/login/", UriKind.Absolute),
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["login"] = _options.Login,
                    ["password"] = _options.Password,
                    ["app"] = "plfpl-web",
                    ["redirect_uri"] = "https://fantasy.premierleague.com/"
                }),
                Headers =
                {
                    {"Origin", "https://fantasy.premierleague.com"},
                    {"Referer", "https://fantasy.premierleague.com"}
                }
            };

            var cookieJar = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieJar,
                UseCookies = true,
                UseDefaultCredentials = false
            };
            var httpClient = new HttpClient(handler);
            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new FplApiException($"Could not authenticate! Status code : {response.StatusCode}");
            }

            var usersCookies = cookieJar.GetAllCookies();
            
            VerifyAuthCookies(usersCookies, "sessionid", "pl_profile", "csrftoken");

            return usersCookies;
        }

        private static void VerifyAuthCookies(List<Cookie> usersCookies, params string[] cookieNames)
        {
            if(!usersCookies.Any())
                throw new FplApiException("No cookies returned!");

            foreach (var cookieName in cookieNames)
            {
                if (!usersCookies.Any(c => c.Name == cookieName))
                {
                    var allCookies = string.Join("\n", usersCookies.OrderBy(c => c.Domain).Select(c => $"[{c.Domain}]{c.Name} : {c.Value}"));
                    var error = $"Missing {cookieName} cookie! Cookies :\n{allCookies}";
                    throw new FplApiException(error);
                }
            }
        }
    }
}