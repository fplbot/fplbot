namespace FplBot.Tests.E2E.Admin;

[Collection("AdminErrorQueue")]
public class AdminErrorQueueFixtureTests(AdminErrorQueueFixture fixture)
{
    // The fault topic's subscription is NOT named per consumer (verified directly against an
    // isolated bus: it's a generic, shared subscription — see the spec's "Ground truth" section).
    // So this checks by topic name (which does encode the message type) and confirms the actual
    // message content by peeking it, rather than filtering subscriptions by an assumed name and
    // trusting ActiveMessageCount (which was also observed to lag behind a real, peekable message
    // shortly after publish in AlmostServiceBus.TestHost).
    [Fact]
    public async Task FaultedMessage_AppearsOnItsFaultTopic()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var (topic, subscription) = await WaitForMessageAsync(key);

        Assert.NotNull(topic);
        Assert.StartsWith("MassTransit/Fault--", topic);

        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }

    // Returns the (topic, subscription) that actually held a message containing `bodyContains`,
    // or (null, null) if none was found within the wait window. Reused by later tasks' tests.
    internal static async Task<(string? Topic, string? Subscription)> WaitForMessageAsync(
        AdminErrorQueueFixture fixture, string bodyContains, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            await foreach (var topic in fixture.AdminClient.GetTopicsAsync())
            {
                if (!topic.Name.StartsWith("MassTransit/Fault--", StringComparison.Ordinal))
                    continue;

                await foreach (var sub in fixture.AdminClient.GetSubscriptionsAsync(topic.Name))
                {
                    await using var receiver = fixture.BusClient.CreateReceiver(topic.Name, sub.SubscriptionName);
                    var peeked = await receiver.PeekMessagesAsync(10);
                    if (peeked.Any(m => m.Body.ToString().Contains(bodyContains, StringComparison.Ordinal)))
                        return (topic.Name, sub.SubscriptionName);
                }
            }
            await Task.Delay(250);
        }
        return (null, null);
    }

    private Task<(string? Topic, string? Subscription)> WaitForMessageAsync(string bodyContains) =>
        WaitForMessageAsync(fixture, bodyContains);
}
