using Discord.Net.Endpoints.Middleware;

namespace FplBot.Tests.UnitTests;

public class DiscordSuccessRedirectTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://evil.example/pwn")]
    [InlineData("//evil.example/pwn")]
    public void NonSiteRelativeState_FallsBackToTheConfiguredSuccessPage(string? state)
    {
        var resolved = DiscordCodeTokenExchangeMiddleware.ResolveSuccessRedirect("/success?type=discord", state);

        Assert.Equal("/success?type=discord", resolved);
    }

    [Fact]
    public void SiteRelativeState_IsUsedAsIsWhenTheSuccessPageIsRelative()
    {
        var resolved = DiscordCodeTokenExchangeMiddleware.ResolveSuccessRedirect("/success?type=discord", "/admin/discord/servers");

        Assert.Equal("/admin/discord/servers", resolved);
    }

    [Fact]
    public void SiteRelativeState_IsResolvedAgainstTheSuccessPageOrigin()
    {
        var resolved = DiscordCodeTokenExchangeMiddleware.ResolveSuccessRedirect("http://localhost:5173/success?type=discord", "/admin/discord/servers");

        Assert.Equal("http://localhost:5173/admin/discord/servers", resolved);
    }
}
