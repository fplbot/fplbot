using FplBot.WebApi.Admin;

namespace FplBot.Tests.E2E.Admin;

// Same rule as Task 2's tests: identify messages by key/content, never by raw Length or
// ActiveMessageCount, because every test in this collection shares the AlwaysFaultsHandler_error
// queue.
[Collection("AdminErrorQueue")]
public class AdminErrorQueueServiceRetryDiscardTests(AdminErrorQueueFixture fixture)
{
    private const string Queue = AdminErrorQueueFixtureTests.ErrorQueueName;

    [Fact]
    public async Task RetryMessageAsync_ReconsumesTheOriginalPayload_AndClearsTheFault()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: false), TestContext.Current.CancellationToken);

        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));
        var message = await WaitForOwnMessageAsync(key);

        var retried = await fixture.Service.RetryMessageAsync(Queue, message.MessageId, TestContext.Current.CancellationToken);
        Assert.True(retried);

        var reprocessed = await WaitForConditionAsync(() => Task.FromResult(AlwaysFaultsHandler.Attempts.GetValueOrDefault(key) >= 2));
        Assert.True(reprocessed, "Expected the retried message to be reconsumed (attempt count >= 2).");

        var stillThere = await MessageStillPresentAsync(key);
        Assert.False(stillThere, "Expected the retried message to be gone from the error queue.");
    }

    [Fact]
    public async Task DiscardMessageAsync_RemovesTheMessage_WithoutReconsuming()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);

        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));
        var message = await WaitForOwnMessageAsync(key);

        var discarded = await fixture.Service.DiscardMessageAsync(Queue, message.MessageId, TestContext.Current.CancellationToken);
        Assert.True(discarded);

        var stillThere = await MessageStillPresentAsync(key);
        Assert.False(stillThere, "Expected the discarded message to be gone from the error queue.");
        Assert.Equal(1, AlwaysFaultsHandler.Attempts[key]);
    }

    [Fact]
    public async Task DiscardMessageAsync_ActsOnANonHeadMessage_NotJustTheQueueHead()
    {
        // Regression test for the cycle-detection race: abandoning the head message makes it
        // immediately redeliverable (this transport delivers in order), so a scan that abandons
        // non-matching messages one at a time as it goes never actually reaches message #2 or #3
        // — it just keeps seeing message #1 and gives up, declaring "not found" even though the
        // real target is sitting deeper in the queue. Publish 3 messages and act on the LAST one
        // published to prove the scan finds it regardless of queue position.
        var keys = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToList();
        foreach (var key in keys)
            await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);

        foreach (var key in keys)
            Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var targetKey = keys[^1];
        var target = await WaitForOwnMessageAsync(targetKey);

        var discarded = await fixture.Service.DiscardMessageAsync(Queue, target.MessageId, TestContext.Current.CancellationToken);
        Assert.True(discarded, "Expected the last-published (non-head) message to be found and discarded.");

        var targetStillThere = await MessageStillPresentAsync(targetKey);
        Assert.False(targetStillThere, "Expected the discarded message to be gone from the error queue.");

        // The other two messages were scanned past but never acted on — confirm they're still
        // present (proving the scan didn't discard everything it touched), then clean them up.
        foreach (var otherKey in keys.Take(2))
        {
            var otherStillThere = await MessagePresentAsync(otherKey);
            Assert.True(otherStillThere, $"Expected the non-target message {otherKey} to remain in the queue.");
            await fixture.DrainMatchingAsync(Queue, otherKey);
        }
    }

    [Fact]
    public async Task ConcurrentActionsOnTheSameQueue_BothFindTheirMessage()
    {
        // Regression test for the false "not found" behind the UI's hang-then-404: these
        // operations receive in PeekLock mode, and a locked message is not handed to anyone else.
        // Two overlapping actions on one queue therefore used to have the second one see an empty
        // queue — everything locked by the first — wait out its receive timeout, and report the
        // message as gone while it was sitting right there. The UI allows this: it only disables
        // the row being acted on, so clicking a second row (or the same one twice) overlaps.
        var keys = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToList();
        foreach (var key in keys)
            await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);

        foreach (var key in keys)
            Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var peeked = await fixture.Service.PeekMessagesAsync(Queue, ct: TestContext.Current.CancellationToken);
        var mine = keys
            .Select(k => peeked.Single(m => m.OriginalMessageJson!.Contains(k, StringComparison.Ordinal)))
            .ToList();

        var first = fixture.Service.DiscardMessageAsync(Queue, mine[0].MessageId, TestContext.Current.CancellationToken);
        var second = fixture.Service.DiscardMessageAsync(Queue, mine[1].MessageId, TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(first, second);

        foreach (var key in keys)
            await fixture.DrainMatchingAsync(Queue, key);

        Assert.True(results[0], "Expected the first of two overlapping discards to find its message.");
        Assert.True(results[1], "Expected the second of two overlapping discards to find its message.");
    }

    private async Task<ErrorQueueMessage> WaitForOwnMessageAsync(string key, int attempts = 100)
    {
        for (var i = 0; i < attempts; i++)
        {
            var messages = await fixture.Service.PeekMessagesAsync(Queue, ct: TestContext.Current.CancellationToken);
            var match = messages.FirstOrDefault(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));
            if (match is not null)
                return match;
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("No peekable message matching this test's key appeared in time.");
    }

    private async Task<bool> MessagePresentAsync(string key)
    {
        var messages = await fixture.Service.PeekMessagesAsync(Queue, ct: TestContext.Current.CancellationToken);
        return messages.Any(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));
    }

    private async Task<bool> MessageStillPresentAsync(string key, int attempts = 60)
    {
        for (var i = 0; i < attempts; i++)
        {
            var messages = await fixture.Service.PeekMessagesAsync(Queue, ct: TestContext.Current.CancellationToken);
            if (messages.All(m => m.OriginalMessageJson == null || !m.OriginalMessageJson.Contains(key)))
                return false;
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        return true;
    }

    private static async Task<bool> WaitForConditionAsync(Func<Task<bool>> check, int attempts = 100)
    {
        for (var i = 0; i < attempts; i++)
        {
            if (await check())
                return true;
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        return false;
    }
}
