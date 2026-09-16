using Azure.Messaging.ServiceBus;

namespace FplBot.Tests.E2E.Admin;

// Every test in this collection publishes the same PoisonTestMessage type and shares one error
// queue (AlwaysFaultsHandler_error), so assertions identify "this test's own message" by its
// `key` (via peek/body content), never by raw Length/ActiveMessageCount.
[Collection("AdminErrorQueue")]
public class AdminErrorQueueServiceListTests(AdminErrorQueueFixture fixture)
{
    [Fact]
    public async Task ListQueuesAsync_IncludesTheErrorQueue()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);

        var found = await AdminErrorQueueFixtureTests.WaitForMessageAsync(
            fixture, AdminErrorQueueFixtureTests.ErrorQueueName, key);
        Assert.True(found);

        var queues = await fixture.Service.ListQueuesAsync(TestContext.Current.CancellationToken);
        var match = queues.FirstOrDefault(q => q.Queue == AdminErrorQueueFixtureTests.ErrorQueueName);

        Assert.NotNull(match);
        Assert.Equal("AlwaysFaultsHandler", match!.Consumer);
        Assert.True(match.Length >= 1);

        await fixture.DrainMatchingAsync(AdminErrorQueueFixtureTests.ErrorQueueName, key);
    }

    [Fact]
    public async Task PeekMessagesAsync_ReturnsFaultDetailsAndOriginalPayload()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true), TestContext.Current.CancellationToken);

        var found = await AdminErrorQueueFixtureTests.WaitForMessageAsync(
            fixture, AdminErrorQueueFixtureTests.ErrorQueueName, key);
        Assert.True(found);

        var messages = await fixture.Service.PeekMessagesAsync(AdminErrorQueueFixtureTests.ErrorQueueName, ct: TestContext.Current.CancellationToken);
        var message = messages.Single(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));

        Assert.Equal("System.InvalidOperationException", message.ExceptionType);
        Assert.Contains("faulted", message.ExceptionMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(nameof(AlwaysFaultsHandler), message.ConsumerType!.Split('.').Last());

        await fixture.DrainMatchingAsync(AdminErrorQueueFixtureTests.ErrorQueueName, key);
    }

    [Fact]
    public async Task PeekMessagesAsync_NonJsonBody_FallsBackToRawText_InsteadOfThrowing()
    {
        var marker = Guid.NewGuid().ToString();
        var rawBody = $"not valid json {marker}";

        await using (var sender = fixture.BusClient.CreateSender(AdminErrorQueueFixtureTests.ErrorQueueName))
        {
            await sender.SendMessageAsync(new ServiceBusMessage(rawBody), TestContext.Current.CancellationToken);
        }

        var found = await AdminErrorQueueFixtureTests.WaitForMessageAsync(
            fixture, AdminErrorQueueFixtureTests.ErrorQueueName, marker);
        Assert.True(found);

        var messages = await fixture.Service.PeekMessagesAsync(AdminErrorQueueFixtureTests.ErrorQueueName, ct: TestContext.Current.CancellationToken);
        var message = messages.Single(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(marker));

        Assert.Equal(rawBody, message.OriginalMessageJson);

        await fixture.DrainMatchingAsync(AdminErrorQueueFixtureTests.ErrorQueueName, marker);
    }
}
