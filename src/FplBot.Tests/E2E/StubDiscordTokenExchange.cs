using System.Net;
using System.Text;

namespace FplBot.Tests.E2E;

public class StubDiscordTokenExchange : HttpMessageHandler
{
    private const string TokenResponse =
        """{"access_token":"token","token_type":"Bearer","guild":{"id":"1","name":"Stub Guild"}}""";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TokenResponse, Encoding.UTF8, "application/json") });
}
