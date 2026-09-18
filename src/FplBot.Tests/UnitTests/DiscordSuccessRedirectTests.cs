using Discord.Net.Endpoints.Middleware;

namespace FplBot.Tests.UnitTests;

public class DiscordSuccessRedirectTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutState_TheSuccessPageIsUsedUntouched(string? state)
    {
        Assert.Equal("/success?type=discord", DiscordCodeTokenExchangeMiddleware.SuccessRedirect("/success?type=discord", state));
    }

    [Fact]
    public void StateRidesBackToTheSuccessPageAsAQueryParameter()
    {
        var redirect = DiscordCodeTokenExchangeMiddleware.SuccessRedirect("/success?type=discord", "/admin/discord/servers");

        Assert.Equal("/success?type=discord&state=%2Fadmin%2Fdiscord%2Fservers", redirect);
    }

    [Theory]
    [InlineData("https://evil.example/pwn")]
    [InlineData("//evil.example/pwn")]
    [InlineData("eyJyZXR1cm5UbyI6Ii9hZG1pbiJ9")]
    public void StateIsNeverTheRedirectTarget_WhateverItHolds(string state)
    {
        var redirect = DiscordCodeTokenExchangeMiddleware.SuccessRedirect("https://fplbot.app/success?type=discord", state);

        Assert.StartsWith("https://fplbot.app/success?type=discord&state=", redirect);
    }
}
