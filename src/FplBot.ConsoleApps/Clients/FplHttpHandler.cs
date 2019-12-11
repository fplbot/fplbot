using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.Options;

namespace FplBot.ConsoleApps
{
    public class FplHttpHandler : HttpClientHandler
    {
        private readonly FplApiClientOptions _options;

        public FplHttpHandler(IOptions<FplApiClientOptions> options)
        {
            _options = options.Value;
            _options.Validate();
            
            AutomaticDecompression = DecompressionMethods.GZip;
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12;   
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sessionCookie = await GetSessionCookie();
            request.Headers.Add("Cookie", sessionCookie);
            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetSessionCookie()
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
                Headers = {
                    { "Origin", "https://fantasy.premierleague.com" },
                    { "Referer", "https://fantasy.premierleague.com" }
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
                throw new FplApiException($"Could not authenticate!");
            }

            var usersCookies = cookieJar.GetAllCookies();
            
            VerifyAuthCookies(usersCookies, "sessionid", "pl_profile", "csrftoken");

            var allCookies = new List<Cookie>();
            allCookies.AddRange(usersCookies);

            return string.Join("; ", allCookies);
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