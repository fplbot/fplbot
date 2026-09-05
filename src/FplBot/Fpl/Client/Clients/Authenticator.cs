using System.Net;
using Microsoft.Extensions.Options;

namespace Fpl.Client.Clients;

public class Authenticator
{
    private readonly FplApiClientOptions _options;

    public Authenticator(IOptions<FplApiClientOptions> options)
    {
        _options = options.Value;
        _options.Validate();
    }

    public async Task<CookieCollection> Authenticate()
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

    private static void VerifyAuthCookies(CookieCollection usersCookies, params string[] cookieNames)
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
