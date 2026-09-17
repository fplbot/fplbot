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

    private async Task<long> RequestedDiscordPermissions()
    {
        var response = await fixture.Get("/api/oauth/install-url-discord");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var redirectUri = JsonDocument.Parse(body).RootElement.GetProperty("redirectUri").GetString()!;

        return long.Parse(QueryHelpers.ParseQuery(new Uri(redirectUri).Query)["permissions"]!);
    }
}
