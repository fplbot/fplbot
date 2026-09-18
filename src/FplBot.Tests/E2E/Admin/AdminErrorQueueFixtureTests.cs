namespace FplBot.Tests.E2E.Admin;

[Collection("AdminErrorQueue")]
public class AdminErrorQueueFixtureTests(AdminErrorQueueFixture fixture)
{
    public const string ErrorQueueName = "AlwaysFaultsHandler_error";

    [Fact]
    public async Task FaultedMessage_LandsInTheErrorQueue()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);

        var found = await WaitForMessageAsync(fixture, ErrorQueueName, key);

        Assert.True(found, $"Expected the faulted PoisonTestMessage to appear in {ErrorQueueName}.");

        await fixture.DrainMatchingAsync(ErrorQueueName, key);
    }

    // Polls the given queue by peeking for a message whose body contains `bodyContains`. Reused
    // by later tasks' tests instead of a bespoke polling loop, and instead of trusting
    // ActiveMessageCount (see Task 2's notes — it reads 0 unconditionally against this emulator).
    internal static async Task<bool> WaitForMessageAsync(
        AdminErrorQueueFixture fixture, string queue, string bodyContains, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            await using var receiver = fixture.BusClient.CreateReceiver(queue);
            var peeked = await receiver.PeekMessagesAsync(10, cancellationToken: TestContext.Current.CancellationToken);
            if (peeked.Any(m => m.Body.ToString().Contains(bodyContains, StringComparison.Ordinal)))
                return true;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        }

        return false;
    }
}
