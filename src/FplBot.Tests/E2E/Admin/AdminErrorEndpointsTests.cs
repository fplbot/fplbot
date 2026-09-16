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
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await AdminErrorEndpoints.GetQueues(fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ErrorQueueSummary>>>(result);
        Assert.Contains(ok.Value!, q => q.Queue == Queue);

        await fixture.DrainMatchingAsync(Queue, key);
    }

    [Fact]
    public async Task RetryMessage_AcceptsImmediately_AndReportsTheOutcomeViaTheJob()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: false), TestContext.Current.CancellationToken);
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));
        var message = (await fixture.Service.PeekMessagesAsync(Queue, ct: TestContext.Current.CancellationToken))
            .Single(m => m.OriginalMessageJson!.Contains(key, StringComparison.Ordinal));

        var accepted = Assert.IsType<Accepted<ErrorQueueJobAccepted>>(
            AdminErrorEndpoints.RetryMessage(Queue, message.MessageId, fixture.Jobs));

        var job = await fixture.WaitForJobAsync(accepted.Value!.JobId);

        Assert.Equal(ErrorQueueJobStatus.Succeeded, job.Status);
        Assert.Equal("Message retried.", job.Message);
    }

    [Fact]
    public async Task RetryMessage_UnknownId_ReportsAFinishedJobSayingTheMessageIsGone()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var accepted = Assert.IsType<Accepted<ErrorQueueJobAccepted>>(
            AdminErrorEndpoints.RetryMessage(Queue, Guid.NewGuid().ToString(), fixture.Jobs));

        var job = await fixture.WaitForJobAsync(accepted.Value!.JobId);

        // The job finishes rather than faulting — "that message is gone" is an outcome to report,
        // not an error — and it must say so in words the operator can act on.
        Assert.Equal(ErrorQueueJobStatus.Succeeded, job.Status);
        Assert.Contains("no longer in the queue", job.Message);

        await fixture.DrainMatchingAsync(Queue, key);
    }

    [Fact]
    public void RetryMessage_NonErrorQueue_ReturnsBadRequest()
    {
        var result = AdminErrorEndpoints.RetryMessage("AlwaysFaultsHandler", "any-id", fixture.Jobs);

        Assert.IsType<BadRequest<object>>(result);
    }

    [Fact]
    public async Task PurgeQueue_ReportsThePurgedCount()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var accepted = Assert.IsType<Accepted<ErrorQueueJobAccepted>>(
            AdminErrorEndpoints.PurgeQueue(Queue, fixture.Jobs));

        var job = await fixture.WaitForJobAsync(accepted.Value!.JobId);

        Assert.Equal(ErrorQueueJobStatus.Succeeded, job.Status);
        Assert.Contains("Purged", job.Message);
    }

    [Fact]
    public void GetJob_UnknownId_ReturnsNotFound()
    {
        var result = AdminErrorEndpoints.GetJob(Guid.NewGuid(), fixture.Jobs);

        Assert.IsType<NotFound>(result);
    }
}
