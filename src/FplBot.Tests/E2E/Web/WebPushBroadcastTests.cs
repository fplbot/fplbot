using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.Tests.E2E.Web;

[Collection("App")]
public class WebPushBroadcastTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.WebPushCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Broadcast_ReachesEverySubscriber()
    {
        var first = $"https://push.example.test/{Guid.NewGuid():N}";
        var second = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: first);
        await fixture.SubscribeToWebPush(leagueId: null, endpoint: second);

        await fixture.Bus.Publish(new BroadcastToWebPush("Heads up", "FplBot has news"),
            TestContext.Current.CancellationToken);

        var firstPush = await fixture.WebPushCapture.WaitForAsync(first);
        var secondPush = await fixture.WebPushCapture.WaitForAsync(second);
        Assert.Equal("Heads up", firstPush.Title);
        Assert.Equal("FplBot has news", secondPush.Body);
    }
}
