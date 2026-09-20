using System.Net;
using System.Net.Http.Headers;
using FplBot.WebApi.Endpoints.Api.Web;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class WebPushEndpointsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Subscribe_WithLeague_ReturnsSubscriberSubscribedToEverythingSupported()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var state = await fixture.GetWebPushState(subscriberId);

        Assert.Equal(123, state.LeagueId);
        Assert.Contains("Standings", state.Events);
        Assert.Contains("FixtureGoals", state.Events);
        Assert.DoesNotContain("Taunts", state.Events);
    }

    [Fact]
    public async Task Subscribe_WithoutLeague_HasNoLeagueRequiringEvents()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: null);

        var state = await fixture.GetWebPushState(subscriberId);

        Assert.Null(state.LeagueId);
        Assert.DoesNotContain("Standings", state.Events);
        Assert.DoesNotContain("Captains", state.Events);
        Assert.Contains("FixtureGoals", state.Events);
    }

    [Fact]
    public async Task PutEvents_Taunts_IsNeverAccepted()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        await fixture.PutWebPush(subscriberId, "/api/web/me/events", new { events = new[] { "Taunts", "Deadlines" } });

        var state = await fixture.GetWebPushState(subscriberId);
        Assert.DoesNotContain("Taunts", state.Events);
        Assert.Contains("Deadlines", state.Events);
    }

    [Fact]
    public async Task PutLeague_Unfollow_DropsLeagueRequiringEvents()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        await fixture.PutWebPush(subscriberId, "/api/web/me/league", new { leagueId = (long?)null });

        var state = await fixture.GetWebPushState(subscriberId);
        Assert.Null(state.LeagueId);
        Assert.DoesNotContain("Standings", state.Events);
        Assert.Contains("PriceChanges", state.Events);
    }

    [Fact]
    public async Task Delete_ThenGet_Returns404()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        await fixture.DeleteWebPush(subscriberId, "/api/web/me");

        var response = await fixture.GetWebPushRaw(subscriberId, "/api/web/me");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownSubscriberId_Returns404()
    {
        var response = await fixture.GetWebPushRaw("does-not-exist", "/api/web/me");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetKey_ReturnsConfiguredVapidPublicKey()
    {
        var key = await fixture.GetJson<VapidKeyResponse>("/api/web/push/key");
        Assert.False(string.IsNullOrWhiteSpace(key.PublicKey));
    }
}
