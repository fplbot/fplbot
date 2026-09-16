using FplBot.WebApi.Admin;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FplBot.Tests.E2E.Admin;

// Same rule as every earlier task's tests: identify this test's own message by its `key`, never
// by raw Length/ActiveMessageCount, since the fault topic is shared across the whole collection.
[Collection("AdminErrorQueue")]
public class AdminErrorEndpointsTests(AdminErrorQueueFixture fixture)
{
    [Fact]
    public async Task GetQueues_ReturnsOkWithTheFaultedQueue()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var result = await AdminErrorEndpoints.GetQueues(fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ErrorQueueSummary>>>(result);
        Assert.Contains(ok.Value!, q => q.Topic == topic && q.Subscription == subscription);

        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }

    [Fact]
    public async Task RetryMessage_UnknownId_ReturnsNotFound()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var result = await AdminErrorEndpoints.RetryMessage(
            Guid.NewGuid().ToString(), topic!, subscription!, fixture.Service, CancellationToken.None);

        Assert.IsType<NotFound>(result);

        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }

    [Fact]
    public async Task PurgeQueue_ReturnsPurgedCount()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var result = await AdminErrorEndpoints.PurgeQueue(topic!, subscription!, fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<PurgeResult>>(result);
        Assert.True(ok.Value!.Purged >= 1);
    }
}
