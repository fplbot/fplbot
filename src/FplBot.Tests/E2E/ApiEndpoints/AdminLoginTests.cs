using System.Net;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Slack;
using Microsoft.AspNetCore.WebUtilities;

namespace FplBot.Tests.E2E.ApiEndpoints;

// Exercises the admin-login provider selection end to end: which OAuth scheme /api/admin/login
// challenges, and — for Discord, the newly added provider — a full challenge/callback round trip
// against a stubbed Discord token/userinfo backchannel (StubDiscordAdminOAuth), asserting the
// resulting session reflects the Discord identity. Slack's own AllowedTeamId/AllowedUserIds gate
// is untouched by this change and isn't re-tested here.
[Collection("App")]
public class AdminLoginTests(AppFixture fixture)
{
    [Fact]
    public async Task Login_WithoutAProvider_DefaultsToSlack()
    {
        var response = await fixture.Get("/api/admin/login");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(SlackAuthenticationDefaults.AuthorizationEndpoint, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_WithProviderSlack_ChallengesSlack()
    {
        var response = await fixture.Get("/api/admin/login?provider=slack");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(SlackAuthenticationDefaults.AuthorizationEndpoint, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_WithProviderDiscord_ChallengesDiscordWithIdentifyAndEmailScopes()
    {
        var response = await fixture.Get("/api/admin/login?provider=discord");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.StartsWith(DiscordAuthenticationDefaults.AuthorizationEndpoint, location.ToString());

        var query = QueryHelpers.ParseQuery(location.Query);
        var scope = query["scope"].ToString();
        Assert.Contains("identify", scope);
        Assert.Contains("email", scope);
        Assert.EndsWith("/signin-discord", query["redirect_uri"].ToString());
    }

    [Fact]
    public async Task DiscordLogin_CompletesOAuthRoundTrip_AndSessionReflectsTheDiscordIdentity()
    {
        var challenge = await fixture.Get("/api/admin/login?provider=discord");
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
        var correlationCookie = CookiePair(challenge, ".AspNetCore.Correlation.");

        var callback = new HttpRequestMessage(
            HttpMethod.Get,
            $"/signin-discord?code=stub-code&state={Uri.EscapeDataString(state)}");
        callback.Headers.Add("Cookie", correlationCookie);
        var callbackResponse = await fixture.Send(callback);

        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        Assert.Equal("/admin", callbackResponse.Headers.Location!.ToString());
        var authCookie = CookiePair(callbackResponse, "fplbot-admin");

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/admin/me");
        meRequest.Headers.Add("Cookie", authCookie);
        var meResponse = await fixture.Send(meRequest);
        var me = await AppFixture.ReadJson<AdminMeDto>(meResponse);

        Assert.Equal("Discord", me.Provider);
        Assert.Equal(StubDiscordAdminOAuth.Email, me.Email);
        Assert.Equal(StubDiscordAdminOAuth.Username, me.Name);
    }

    private static string CookiePair(HttpResponseMessage response, string namePrefix) =>
        response.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(namePrefix)).Split(';')[0];

    private record AdminMeDto(string? Name, string? Email, string? Provider, bool IsAdmin);
}
