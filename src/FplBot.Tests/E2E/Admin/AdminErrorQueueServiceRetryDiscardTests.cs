using FplBot.WebApi.Admin;

namespace FplBot.Tests.E2E.Admin;

// Like Task 2's tests, every assertion here identifies "this test's own message" by its `key`
// (never by raw queue Length or ActiveMessageCount — see Task 1/2's notes on why), because every
// test in this collection shares one fault topic per message type.
[Collection("AdminErrorQueue")]
public class AdminErrorQueueServiceRetryDiscardTests(AdminErrorQueueFixture fixture)
{
    [Fact]
    public async Task RetryMessageAsync_ReconsumesTheOriginalPayload_AndClearsTheFault()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: false));

        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);
        var message = await WaitForOwnMessageAsync(topic!, subscription!, key);

        var retried = await fixture.Service.RetryMessageAsync(topic!, subscription!, message.MessageId);
        Assert.True(retried);

        var reprocessed = await WaitForConditionAsync(() => Task.FromResult(AlwaysFaultsHandler.Attempts.GetValueOrDefault(key) >= 2));
        Assert.True(reprocessed, "Expected the retried message to be reconsumed (attempt count >= 2).");

        var stillThere = await MessageStillPresentAsync(topic!, subscription!, key);
        Assert.False(stillThere, "Expected the retried message to be gone from the fault subscription.");
    }

    [Fact]
    public async Task DiscardMessageAsync_RemovesTheMessage_WithoutReconsuming()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);
        var message = await WaitForOwnMessageAsync(topic!, subscription!, key);

        var discarded = await fixture.Service.DiscardMessageAsync(topic!, subscription!, message.MessageId);
        Assert.True(discarded);

        var stillThere = await MessageStillPresentAsync(topic!, subscription!, key);
        Assert.False(stillThere, "Expected the discarded message to be gone from the fault subscription.");
        Assert.Equal(1, AlwaysFaultsHandler.Attempts[key]);
    }

    [Fact]
    public async Task RetryMessageAsync_UnknownMessageId_ReturnsFalse()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);
        await WaitForOwnMessageAsync(topic!, subscription!, key);

        var result = await fixture.Service.RetryMessageAsync(topic!, subscription!, Guid.NewGuid().ToString());

        Assert.False(result);

        // A non-matching messageId leaves every real message untouched (abandoned back) — drain
        // this test's own message so it doesn't leak into later tests' shared-topic assertions.
        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }

    private async Task<ErrorQueueMessage> WaitForOwnMessageAsync(string topic, string subscription, string key, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            var messages = await fixture.Service.PeekMessagesAsync(topic, subscription);
            var match = messages.FirstOrDefault(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));
            if (match is not null)
                return match;
            await Task.Delay(250);
        }
        throw new TimeoutException("No peekable message matching this test's key appeared in time.");
    }

    private async Task<bool> MessageStillPresentAsync(string topic, string subscription, string key, int attempts = 12)
    {
        for (var i = 0; i < attempts; i++)
        {
            var messages = await fixture.Service.PeekMessagesAsync(topic, subscription);
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
