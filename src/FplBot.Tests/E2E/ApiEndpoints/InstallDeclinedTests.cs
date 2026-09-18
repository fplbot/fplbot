using System.Net;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class InstallDeclinedTests(AppFixture fixture)
{
    private const string CancelledPage = "http://localhost:5173/install-cancelled";

    [Theory]
    [InlineData("/oauth/authorize")]
    [InlineData("/oauth/discord/authorize")]
    public async Task DecliningTheConsentScreen_LandsOnTheCancelledPage(string callback)
    {
        var response = await fixture.Get($"{callback}?error=access_denied&error_description=The+user+said+no");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"{CancelledPage}?reason=access_denied", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("/oauth/authorize")]
    [InlineData("/oauth/discord/authorize")]
    public async Task DecliningKeepsTheStateTheInstallStartedWith(string callback)
    {
        var response = await fixture.Get($"{callback}?error=access_denied&state=%2Fadmin%2Fslack");

        Assert.Equal($"{CancelledPage}?reason=access_denied&state=%2Fadmin%2Fslack",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AnInstallThatFailsForAnotherReason_KeepsTheStateToo()
    {
        var response = await fixture.Get("/oauth/discord/authorize?state=%2Fadmin%2Fdiscord%2Fservers");

        Assert.Equal($"{CancelledPage}?details=no_code&state=%2Fadmin%2Fdiscord%2Fservers",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AnInstallThatWasNotDeclined_IsLeftToTheInstallMiddleware()
    {
        var response = await fixture.Get("/oauth/discord/authorize?code=the-code&guild_id=1");

        Assert.Equal("http://localhost:5173/success?type=discord", response.Headers.Location!.ToString());
    }
}
