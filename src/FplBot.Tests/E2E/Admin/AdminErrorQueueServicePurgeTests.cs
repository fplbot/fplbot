namespace FplBot.Tests.E2E.Admin;

// Same rule as Tasks 2/3: identify messages by key/content, never by raw Length or
// ActiveMessageCount, because every test in this collection shares the AlwaysFaultsHandler_error
// queue.
[Collection("AdminErrorQueue")]
public class AdminErrorQueueServicePurgeTests(AdminErrorQueueFixture fixture)
{
    private const string Queue = AdminErrorQueueFixtureTests.ErrorQueueName;

    [Fact]
    public async Task PurgeQueueAsync_RemovesEveryMessage_InOneCall()
    {
        var keys = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToList();
        foreach (var key in keys)
            await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);

        foreach (var key in keys)
            Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var purged = await fixture.Service.PurgeQueueAsync(Queue, TestContext.Current.CancellationToken);

        // >= rather than == : purge legitimately clears every message currently in the shared
        // queue, including any stray leftovers from an earlier test's failed cleanup — this test
        // only needs to know its own 3 keys are gone, not that purged is exactly 3.
        Assert.True(purged >= keys.Count, $"Expected at least {keys.Count} messages purged, got {purged}.");
        foreach (var key in keys)
        {
            var messages = await fixture.Service.PeekMessagesAsync(Queue, ct: TestContext.Current.CancellationToken);
            Assert.DoesNotContain(messages, m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));
        }
    }
}
