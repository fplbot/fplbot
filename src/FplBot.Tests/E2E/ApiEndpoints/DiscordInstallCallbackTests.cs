using System.Net;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class DiscordInstallCallbackTests(AppFixture fixture)
{
    // Development serves the SPA from the vite dev server, so the success page is absolute there.
    private const string SuccessPage = "http://localhost:5173/success?type=discord";

    [Fact]
    public async Task AnInstallWithoutState_LandsOnTheSuccessPage()
    {
        var response = await fixture.Get("/oauth/discord/authorize?code=the-code&guild_id=1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(SuccessPage, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task TheStateTheInstallStartedWith_ComesBackOnTheSuccessPage()
    {
        var response = await fixture.Get("/oauth/discord/authorize?code=the-code&guild_id=1&state=%2Fadmin%2Fdiscord%2Fservers");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"{SuccessPage}&state=%2Fadmin%2Fdiscord%2Fservers", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AStateHoldingAnythingElse_IsStillHandedBackUntouched()
    {
        var response = await fixture.Get("/oauth/discord/authorize?code=the-code&guild_id=1&state=eyJhIjoxfQ");

        Assert.Equal($"{SuccessPage}&state=eyJhIjoxfQ", response.Headers.Location!.ToString());
    }
}
