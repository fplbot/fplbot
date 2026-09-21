using System.Net;
using System.Text;

namespace FplBot.Tests.E2E;

// Stands in for Discord's OAuth token + userinfo endpoints during the admin-login flow
// (AspNet.Security.OAuth.Discord), so DiscordLoginTests can drive a real challenge/callback
// round trip without a network call to discord.com.
public class StubDiscordAdminOAuth : HttpMessageHandler
{
    public const string Email = "discordadmin@example.test";
    public const string Username = "discordadmin";

    private const string TokenResponse = """{"access_token":"stub-access-token","token_type":"Bearer"}""";
    private static readonly string UserResponse =
        $$"""{"id":"999","username":"{{Username}}","email":"{{Email}}","avatar":null,"discriminator":"0"}""";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.RequestUri!.AbsolutePath.Contains("/oauth2/token") ? TokenResponse : UserResponse;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}
