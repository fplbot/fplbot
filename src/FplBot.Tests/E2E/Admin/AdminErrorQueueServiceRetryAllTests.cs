namespace FplBot.Tests.E2E.Admin;

// Same rule as the other tests in this collection: identify messages by key/content, never by raw
// Length or ActiveMessageCount, because the error queue is shared across the whole collection.
[Collection("AdminErrorQueue")]
public class AdminErrorQueueServiceRetryAllTests(AdminErrorQueueFixture fixture)
{
    private const string Queue = AdminErrorQueueFixtureTests.ErrorQueueName;

    [Fact]
    public async Task RetryAllMessagesAsync_ReconsumesEveryMessage_AndEmptiesTheQueue()
    {
        var keys = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToList();
        foreach (var key in keys)
            await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: false), TestContext.Current.CancellationToken);

        foreach (var key in keys)
            Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var retried = await fixture.Service.RetryAllMessagesAsync(Queue, TestContext.Current.CancellationToken);

        // >= rather than ==, for the same reason purge asserts that way: the drain legitimately
        // covers every message in the shared queue, including strays from an earlier test.
        Assert.True(retried >= keys.Count, $"Expected at least {keys.Count} messages retried, got {retried}.");

        foreach (var key in keys)
        {
            var reprocessed = await WaitForConditionAsync(() => AlwaysFaultsHandler.Attempts.GetValueOrDefault(key) >= 2);
            Assert.True(reprocessed, $"Expected {key} to be reconsumed after retry-all.");
        }

        var remaining = await fixture.Service.PeekMessagesAsync(Queue, ct: TestContext.Current.CancellationToken);
        foreach (var key in keys)
            Assert.DoesNotContain(remaining, m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));
    }

    [Fact]
    public async Task RetryAllMessagesAsync_StillFaultingConsumer_DoesNotReprocessItsOwnOutput()
    {
        // Every resend of an always-faulting message lands straight back in this same queue. The
        // drain is bounded by the time it started, so those re-faults are left for the operator's
        // next click instead of being picked up again in the same run (which would never end).
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var retried = await fixture.Service.RetryAllMessagesAsync(Queue, TestContext.Current.CancellationToken);

        Assert.True(retried >= 1, $"Expected at least this test's message to be retried, got {retried}.");
        var attempts = await WaitForConditionAsync(() => AlwaysFaultsHandler.Attempts.GetValueOrDefault(key) >= 2);
        Assert.True(attempts, "Expected the message to be redelivered to the consumer.");

        // It faulted again, so it is back in the error queue — the retry ran, it just failed again.
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));
        await fixture.DrainMatchingAsync(Queue, key);
    }

    private static async Task<bool> WaitForConditionAsync(Func<bool> check, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            if (check())
                return true;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        }

        return false;
    }
}
