namespace FplBot.Tests.E2E.Admin;

[Collection("AdminErrorQueue")]
public class AdminErrorQueueServiceListTests(AdminErrorQueueFixture fixture)
{
    // Every test in this collection shares one fault topic per message type (see Task 1's fixture
    // notes) — so assertions here match by this test's own `key` (via peek/body content) rather
    // than by an exact queue Length, which reflects everything currently in the shared topic, and
    // rather than by ActiveMessageCount, which was observed to lag behind a real, peekable message.
    [Fact]
    public async Task ListQueuesAsync_IncludesQueueForTheFaultedMessageType()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var queues = await fixture.Service.ListQueuesAsync();
        var found = queues.FirstOrDefault(q => q.Topic == topic && q.Subscription == subscription);

        Assert.NotNull(found);
        Assert.Contains(nameof(PoisonTestMessage), found!.MessageType);
        Assert.True(found.Length >= 1);

        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }

    [Fact]
    public async Task PeekMessagesAsync_ReturnsFaultDetailsAndOriginalPayload()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var messages = await fixture.Service.PeekMessagesAsync(topic!, subscription!);
        var message = messages.Single(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));

        Assert.NotEmpty(message.Exceptions);
        Assert.Contains("faulted", message.Exceptions[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(nameof(AlwaysFaultsHandler), message.SourceAddress);

        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }
}
