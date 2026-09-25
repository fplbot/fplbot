using System.Net;
using System.Net.Http.Headers;
using FplBot.WebApi.Endpoints.Api.Admin;
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
    public async Task Subscribe_WithEntry_CoexistsWithLeague()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, entryId: 456);

        var state = await fixture.GetWebPushState(subscriberId);

        Assert.Equal(123, state.LeagueId);
        Assert.Equal(456, state.EntryId);
    }

    [Fact]
    public async Task Subscribe_WithoutEntry_LeavesEntryNull()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var state = await fixture.GetWebPushState(subscriberId);

        Assert.Null(state.EntryId);
    }

    [Fact]
    public async Task Subscribe_EntryIdBeyondIntRange_IsRejected()
    {
        var response = await fixture.Post("/api/web/push/subscribe",
            new { endpoint = "https://push.example.test/abc", p256dh = "y", auth = "z", entryId = 4294967297L });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutEntry_LinksEntryWithoutAffectingLeague()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var response = await fixture.PutWebPush(subscriberId, "/api/web/me/entry", new { entryId = 456L });

        response.EnsureSuccessStatusCode();
        var state = await fixture.GetWebPushState(subscriberId);
        Assert.Equal(456, state.EntryId);
        Assert.Equal(123, state.LeagueId);
    }

    [Fact]
    public async Task PutEntry_Unlink_ClearsEntryOnly()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, entryId: 456);

        await fixture.PutWebPush(subscriberId, "/api/web/me/entry", new { entryId = (long?)null });

        var state = await fixture.GetWebPushState(subscriberId);
        Assert.Null(state.EntryId);
        Assert.Equal(123, state.LeagueId);
    }

    [Fact]
    public async Task PutEntry_EntryIdBeyondIntRange_IsRejectedAndKeepsCurrentEntry()
    {
        var subscriberId = await fixture.SubscribeToWebPush(entryId: 456);

        var response = await fixture.PutWebPush(subscriberId, "/api/web/me/entry", new { entryId = 4294967297L });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var state = await fixture.GetWebPushState(subscriberId);
        Assert.Equal(456, state.EntryId);
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
    public async Task Subscribe_NonUriEndpoint_IsRejectedAndStoresNothing()
    {
        var response = await fixture.Post("/api/web/push/subscribe", new { endpoint = "x", p256dh = "y", auth = "z" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var page = await fixture.GetJson<PagedResult<SubscriberSummaryDto>>("/api/admin/web/subscribers?page=0&pageSize=20");
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task Subscribe_NonHttpsEndpoint_IsRejected()
    {
        var response = await fixture.Post("/api/web/push/subscribe",
            new { endpoint = "http://push.example.test/abc", p256dh = "y", auth = "z" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Subscribe_LeagueIdBeyondIntRange_IsRejected()
    {
        var response = await fixture.Post("/api/web/push/subscribe",
            new { endpoint = "https://push.example.test/abc", p256dh = "y", auth = "z", leagueId = 4294967297L });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutLeague_LeagueIdBeyondIntRange_IsRejectedAndKeepsCurrentLeague()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var response = await fixture.PutWebPush(subscriberId, "/api/web/me/league", new { leagueId = 4294967297L });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var state = await fixture.GetWebPushState(subscriberId);
        Assert.Equal(123, state.LeagueId);
    }

    [Fact]
    public async Task PutLeague_AfterOptingOutOfEverything_DoesNotReenableEvents()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: null);
        var optOut = await fixture.PutWebPush(subscriberId, "/api/web/me/events", new { events = Array.Empty<string>() });
        optOut.EnsureSuccessStatusCode();

        var followed = await fixture.PutWebPush(subscriberId, "/api/web/me/league", new { leagueId = 123L });
        followed.EnsureSuccessStatusCode();

        var state = await fixture.GetWebPushState(subscriberId);
        Assert.Equal(123, state.LeagueId);
        Assert.Empty(state.Events);
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
