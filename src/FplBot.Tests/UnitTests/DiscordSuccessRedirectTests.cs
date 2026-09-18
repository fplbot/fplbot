using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;

namespace FplBot.Tests.UnitTests;

public class DiscordSuccessRedirectTests
{
    [Fact]
    public void WithNoResolver_TheStateIsIgnored()
    {
        var options = new DiscordOAuthOptions { SuccessRedirectUri = "/success?type=discord" };

        Assert.Equal("/success?type=discord", DiscordCodeTokenExchangeMiddleware.SuccessRedirect(options, "/admin/discord/servers"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://evil.example/pwn")]
    [InlineData("//evil.example/pwn")]
    [InlineData("/\\evil.example/pwn")]
    [InlineData("/admin\tservers")]
    [InlineData("admin/discord/servers")]
    public void AResolvedTargetThatIsNotASiteRelativePath_FallsBackToTheSuccessPage(string? state)
    {
        var options = Options("/success?type=discord");

        Assert.Equal("/success?type=discord", DiscordCodeTokenExchangeMiddleware.SuccessRedirect(options, state));
    }

    [Fact]
    public void ASiteRelativeTarget_IsUsedAsIsWhenTheSuccessPageIsRelative()
    {
        var options = Options("/success?type=discord");

        Assert.Equal("/admin/discord/servers", DiscordCodeTokenExchangeMiddleware.SuccessRedirect(options, "/admin/discord/servers"));
    }

    [Fact]
    public void ASiteRelativeTarget_IsResolvedAgainstTheSuccessPageOrigin()
    {
        var options = Options("http://localhost:5173/success?type=discord");

        Assert.Equal("http://localhost:5173/admin/discord/servers", DiscordCodeTokenExchangeMiddleware.SuccessRedirect(options, "/admin/discord/servers"));
    }

    [Fact]
    public void TheResolverDecidesWhatTheStateMeans()
    {
        var options = new DiscordOAuthOptions
        {
            SuccessRedirectUri = "/success?type=discord",
            ResolveSuccessRedirect = state => state == "nonce-42" ? "/admin/discord/servers" : null
        };

        Assert.Equal("/admin/discord/servers", DiscordCodeTokenExchangeMiddleware.SuccessRedirect(options, "nonce-42"));
        Assert.Equal("/success?type=discord", DiscordCodeTokenExchangeMiddleware.SuccessRedirect(options, "/some/path"));
    }

    private static DiscordOAuthOptions Options(string successRedirectUri) =>
        new() { SuccessRedirectUri = successRedirectUri, ResolveSuccessRedirect = state => state };
}
