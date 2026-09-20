using System.Net;
using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.Tests.E2E.Web;

[Collection("App")]
public class WebPushDeliveryTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.WebPushCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Command_DeliversTitleBodyAndLeagueLink()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new PublishToWebPushSubscriber(subscriberId, "Title", "Body", 123),
            TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Equal("Title", push.Title);
        Assert.Equal("Body", push.Body);
        Assert.Equal("/leagues/123", push.Link);
    }

    [Fact]
    public async Task Gone_DeletesTheSubscriber()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);
        fixture.WebPushCapture.FailEndpointAsGone(endpoint);

        await fixture.Bus.Publish(new PublishToWebPushSubscriber(subscriberId, "Title", "Body", 123),
            TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle();

        var response = await fixture.GetWebPushRaw(subscriberId, "/api/web/me");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OneDeadEndpoint_DoesNotStopTheOther()
    {
        var deadEndpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var liveEndpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var dead = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: deadEndpoint);
        var live = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: liveEndpoint);
        fixture.WebPushCapture.FailEndpointAsGone(deadEndpoint);

        await fixture.Bus.Publish(new PublishToWebPushSubscriber(dead, "Title", "Body", 123),
            TestContext.Current.CancellationToken);
        await fixture.Bus.Publish(new PublishToWebPushSubscriber(live, "Title", "Body", 123),
            TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(liveEndpoint);
        Assert.Equal("Title", push.Title);
    }
}
