namespace Fpl.Client.Clients;

public class FplDelegatingHandler(CookieFetcher cookieFetcher) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sessionCookie = await cookieFetcher.GetSessionCookie();
        request.Headers.Add("Cookie", sessionCookie);


        return await base.SendAsync(request, cancellationToken);
    }
}
