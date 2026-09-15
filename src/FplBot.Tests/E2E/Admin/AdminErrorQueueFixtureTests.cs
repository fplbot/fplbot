namespace FplBot.Tests.E2E.Admin;

[Collection("AdminErrorQueue")]
public class AdminErrorQueueFixtureTests(AdminErrorQueueFixture fixture)
{
    [Fact]
    public async Task FaultedMessage_AppearsAsActiveMessageOnItsFaultSubscription()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var found = await WaitForConditionAsync(async () =>
        {
            await foreach (var topic in fixture.AdminClient.GetTopicsAsync())
            {
                if (!topic.Name.StartsWith("MassTransit/Fault--", StringComparison.Ordinal))
                    continue;

                await foreach (var sub in fixture.AdminClient.GetSubscriptionsAsync(topic.Name))
                {
                    if (sub.SubscriptionName != nameof(AlwaysFaultsConsumer))
                        continue;

                    var runtime = await fixture.AdminClient.GetSubscriptionRuntimePropertiesAsync(topic.Name, sub.SubscriptionName);
                    if (runtime.Value.ActiveMessageCount > 0)
                        return true;
                }
            }
            return false;
        });

        Assert.True(found, "Expected the faulted PoisonTestMessage to appear on AlwaysFaultsConsumer's fault subscription.");
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
