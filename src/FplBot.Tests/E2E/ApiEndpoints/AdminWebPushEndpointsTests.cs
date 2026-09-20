using System.Net;
using FplBot.WebApi.Endpoints.Api.Admin;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class AdminWebPushEndpointsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.WebPushCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task List_ReturnsSubscribersWithEndpointHostOnly()
    {
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: "https://push.example.test/abc", name: "iPhone");

        var response = await fixture.Get("/api/admin/web/subscribers?page=0&pageSize=20");
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var page = await AppFixture.ReadJson<PagedResult<SubscriberSummaryDto>>(response);

        var item = Assert.Single(page.Items);
        Assert.Equal("iPhone", item.Name);
        Assert.Equal(123, item.LeagueId);
        Assert.Equal("push.example.test", item.EndpointHost);
        Assert.DoesNotContain("/abc", raw);
        Assert.DoesNotContain("BFakeP256dhKeyForTests", raw);
        Assert.DoesNotContain("FakeAuthSecret", raw);
    }

    [Fact]
    public async Task List_PagesThroughSubscribers()
    {
        await fixture.SubscribeToWebPush();
        await fixture.SubscribeToWebPush();
        await fixture.SubscribeToWebPush();

        var firstPage = await fixture.GetJson<PagedResult<SubscriberSummaryDto>>("/api/admin/web/subscribers?page=0&pageSize=2");
        var secondPage = await fixture.GetJson<PagedResult<SubscriberSummaryDto>>("/api/admin/web/subscribers?page=1&pageSize=2");

        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Single(secondPage.Items);
        Assert.DoesNotContain(secondPage.Items, s => firstPage.Items.Any(f => f.Id == s.Id));
    }

    [Fact]
    public async Task GetSingle_ReturnsTheSubscriber()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 456, name: "Pixel");

        var subscriber = await fixture.GetJson<SubscriberSummaryDto>($"/api/admin/web/subscribers/{subscriberId}");

        Assert.Equal(subscriberId, subscriber.Id);
        Assert.Equal("Pixel", subscriber.Name);
        Assert.Equal(456, subscriber.LeagueId);
    }

    [Fact]
    public async Task GetSingle_UnknownSubscriber_ReturnsNotFound()
    {
        var response = await fixture.Get("/api/admin/web/subscribers/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheSubscriber()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var deleted = await fixture.Delete($"/api/admin/web/subscribers/{subscriberId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var page = await fixture.GetJson<PagedResult<SubscriberSummaryDto>>("/api/admin/web/subscribers?page=0&pageSize=20");
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Broadcast_SendsToSubscribers()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        var response = await fixture.Post("/api/admin/web/broadcast", new { title = "Heads up", body = "FplBot has news" });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Equal("Heads up", push.Title);
        Assert.Equal("FplBot has news", push.Body);
    }
}
