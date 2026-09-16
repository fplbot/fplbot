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
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: false));

        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));
        var message = await WaitForOwnMessageAsync(key);

        var retried = await fixture.Service.RetryMessageAsync(Queue, message.MessageId);
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
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));
        var message = await WaitForOwnMessageAsync(key);

        var discarded = await fixture.Service.DiscardMessageAsync(Queue, message.MessageId);
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
            await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        foreach (var key in keys)
            Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var targetKey = keys[^1];
        var target = await WaitForOwnMessageAsync(targetKey);

        var discarded = await fixture.Service.DiscardMessageAsync(Queue, target.MessageId);
        Assert.True(discarded, "Expected the last-published (non-head) message to be found and discarded.");

        var targetStillThere = await MessageStillPresentAsync(targetKey);
        Assert.False(targetStillThere, "Expected the discarded message to be gone from the error queue.");

        // The other two messages were scanned past but never acted on — confirm they're still
        // present (proving the scan didn't discard everything it touched), then clean them up.
        foreach (var otherKey in keys.Take(2))
        {
            var otherStillThere = await MessageStillPresentAsync(otherKey);
            Assert.True(otherStillThere, $"Expected the non-target message {otherKey} to remain in the queue.");
            await fixture.DrainMatchingAsync(Queue, otherKey);
        }
    }

    [Fact]
    public async Task RetryMessageAsync_UnknownMessageId_ReturnsFalse()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await fixture.Service.RetryMessageAsync(Queue, Guid.NewGuid().ToString());

        Assert.False(result);

        // A non-matching messageId leaves every real message untouched (abandoned back) — drain
        // this test's own message so it doesn't leak into later tests.
        await fixture.DrainMatchingAsync(Queue, key);
    }

    private async Task<ErrorQueueMessage> WaitForOwnMessageAsync(string key, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            var messages = await fixture.Service.PeekMessagesAsync(Queue);
            var match = messages.FirstOrDefault(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));
            if (match is not null)
                return match;
            await Task.Delay(250);
        }
        throw new TimeoutException("No peekable message matching this test's key appeared in time.");
    }

    private async Task<bool> MessageStillPresentAsync(string key, int attempts = 12)
    {
        for (var i = 0; i < attempts; i++)
        {
            var messages = await fixture.Service.PeekMessagesAsync(Queue);
            if (messages.All(m => m.OriginalMessageJson == null || !m.OriginalMessageJson.Contains(key)))
                return false;
            await Task.Delay(250);
        }
        return true;
    }

    private static async Task<bool> WaitForConditionAsync(Func<Task<bool>> check, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            if (await check())
                return true;
            await Task.Delay(250);
        }
        return false;
    }
}
