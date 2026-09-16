using FplBot.WebApi.Admin;

namespace FplBot.Tests.E2E.Admin;

// Same rule as Tasks 2/3: identify messages by key/content, never by raw Length or
// ActiveMessageCount, because every test in this collection shares one fault topic per message
// type.
[Collection("AdminErrorQueue")]
public class AdminErrorQueueServicePurgeTests(AdminErrorQueueFixture fixture)
{
    [Fact]
    public async Task PurgeQueueAsync_RemovesEveryMessage_InOneCall()
    {
        var keys = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToList();
        foreach (var key in keys)
            await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, keys[0]);
        Assert.NotNull(topic);
        foreach (var key in keys.Skip(1))
            await WaitForOwnMessageAsync(topic!, subscription!, key);

        var purged = await fixture.Service.PurgeQueueAsync(topic!, subscription!);

        // >= rather than == : purge legitimately clears every message currently in the shared
        // topic, including any stray leftovers from an earlier test's failed cleanup — this test
        // only needs to know its own 3 keys are gone, not that purged is exactly 3.
        Assert.True(purged >= keys.Count, $"Expected at least {keys.Count} messages purged, got {purged}.");
        foreach (var key in keys)
        {
            var stillThere = await MessageStillPresentAsync(topic!, subscription!, key);
            Assert.False(stillThere, $"Expected key {key} to be gone after purge.");
        }
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
}
