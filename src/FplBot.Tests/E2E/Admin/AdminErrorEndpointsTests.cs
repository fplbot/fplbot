using FplBot.WebApi.Admin;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FplBot.Tests.E2E.Admin;

// Same rule as every earlier task's tests: identify this test's own message by its `key`, never
// by raw Length/ActiveMessageCount, since the error queue is shared across the whole collection.
[Collection("AdminErrorQueue")]
public class AdminErrorEndpointsTests(AdminErrorQueueFixture fixture)
{
    private const string Queue = AdminErrorQueueFixtureTests.ErrorQueueName;

    [Fact]
    public async Task GetQueues_ReturnsOkWithTheErrorQueue()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await AdminErrorEndpoints.GetQueues(fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ErrorQueueSummary>>>(result);
        Assert.Contains(ok.Value!, q => q.Queue == Queue);

        await fixture.DrainMatchingAsync(Queue, key);
    }

    [Fact]
    public async Task RetryMessage_UnknownId_ReturnsNotFound()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await AdminErrorEndpoints.RetryMessage(Queue, Guid.NewGuid().ToString(), fixture.Service, CancellationToken.None);

        Assert.IsType<NotFound>(result);

        await fixture.DrainMatchingAsync(Queue, key);
    }

    [Fact]
    public async Task RetryMessage_NonErrorQueue_ReturnsBadRequest()
    {
        var result = await AdminErrorEndpoints.RetryMessage("AlwaysFaultsHandler", "any-id", fixture.Service, CancellationToken.None);

        Assert.IsType<BadRequest<object>>(result);
    }

    [Fact]
    public async Task PurgeQueue_ReturnsPurgedCount()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await AdminErrorEndpoints.PurgeQueue(Queue, fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<PurgeResult>>(result);
        Assert.True(ok.Value!.Purged >= 1);
    }
}
