using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class InstallUrlEndpointsTests(AppFixture fixture)
{
    private const long ViewChannel = 1L << 10;
    private const long SendMessages = 1L << 11;
    private const long EmbedLinks = 1L << 14;

    [Fact]
    public async Task DiscordInstallUrl_AsksForThePermissionsTheBotPostsWith()
    {
        var permissions = await RequestedDiscordPermissions();

        Assert.True((permissions & ViewChannel) != 0, "bot cannot reach a channel it cannot see");
        Assert.True((permissions & SendMessages) != 0, "bot cannot deliver notifications without it");
        Assert.True((permissions & EmbedLinks) != 0, "all but the deadline notifications are rich embeds");
    }

    [Theory]
    [InlineData("/admin/slack", "/admin/slack")]
    [InlineData("https://evil.example/pwn", null)]
    [InlineData("//evil.example/pwn", null)]
    [InlineData("/\\evil.example/pwn", null)]
    [InlineData(null, null)]
    public async Task SlackInstallUrl_CarriesOnlyASiteRelativeReturnPathAsState(string? returnTo, string? expectedState)
    {
        Assert.Equal(expectedState, await StateOn("/api/oauth/install-url/slack", returnTo));
    }

    [Theory]
    [InlineData("/admin/discord/servers", "/admin/discord/servers")]
    [InlineData("https://evil.example/pwn", null)]
    [InlineData("//evil.example/pwn", null)]
    [InlineData("/\\evil.example/pwn", null)]
    [InlineData(null, null)]
    public async Task DiscordInstallUrl_CarriesOnlyASiteRelativeReturnPathAsState(string? returnTo, string? expectedState)
    {
        Assert.Equal(expectedState, await StateOn("/api/oauth/install-url/discord", returnTo));
    }

    private async Task<string?> StateOn(string path, string? returnTo)
    {
        var query = returnTo is null ? "" : $"?returnTo={Uri.EscapeDataString(returnTo)}";
        var response = await fixture.Get($"{path}{query}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var redirectUri = JsonDocument.Parse(body).RootElement.GetProperty("redirectUri").GetString()!;
        return QueryHelpers.ParseQuery(new Uri(redirectUri).Query).TryGetValue("state", out var s) ? s.ToString() : null;
    }

    private async Task<long> RequestedDiscordPermissions()
    {
        var response = await fixture.Get("/api/oauth/install-url/discord");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var redirectUri = JsonDocument.Parse(body).RootElement.GetProperty("redirectUri").GetString()!;

        return long.Parse(QueryHelpers.ParseQuery(new Uri(redirectUri).Query)["permissions"]!);
    }
}
