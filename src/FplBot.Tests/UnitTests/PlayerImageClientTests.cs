using System.Net;
using Fpl.Client;

namespace FplBot.Tests.UnitTests;

public class PlayerImageClientTests
{
    [Fact]
    public async Task ExistingPhoto_IsUsed()
    {
        var client = ClientReturning(HttpStatusCode.OK);

        var url = await client.GetPlayerImageUrl(118748);

        Assert.Equal("https://platform-static-files.s3.amazonaws.com/premierleague/photos/players/110x140/p118748.png", url);
    }

    [Fact]
    public async Task MissingPhoto_FallsBackToPlaceholder()
    {
        var client = ClientReturning(HttpStatusCode.Forbidden);

        var url = await client.GetPlayerImageUrl(118748);

        Assert.Equal("https://user-images.githubusercontent.com/206726/73577018-207e4100-447c-11ea-98e3-9cc598c56519.png", url);
    }

    private static PlayerImageClient ClientReturning(HttpStatusCode statusCode) =>
        new(new HttpClient(new StubHttpMessageHandler(statusCode))
        {
            BaseAddress = new Uri("https://platform-static-files.s3.amazonaws.com/")
        });

    private class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
