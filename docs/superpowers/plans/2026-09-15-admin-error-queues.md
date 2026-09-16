# Admin Error Queues Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an "Errors" tab to the fplbot admin UI that lists MassTransit's existing fault queues (one row per faulted message *type*, i.e. per `MassTransit/Fault--<type>--` topic+subscription pair), lets an admin inspect a faulted message's details and body — including which consumer actually faulted, read per-message from `sourceAddress` — retry it, discard it, or purge an entire queue.

**Architecture:** A new `AdminErrorQueueService` in the WebApi service talks directly to Azure Service Bus via the `Azure.Messaging.ServiceBus` SDK (list/peek/retry/discard/purge against `MassTransit/Fault--<type>--` topics and their subscription(s) — the mechanism that already produces fault visibility today, unchanged). Note: the fault topic's subscription is **not** named per consumer (verified directly — it's a generic, shared subscription); grouping is by message type, and the actual faulting consumer is only knowable per-message via `sourceAddress`. New minimal-API endpoints under `/api/admin/errors/**` expose it; a new Vue admin tab consumes those endpoints. No MassTransit pipeline configuration changes.

**Tech Stack:** .NET 11 minimal APIs, MassTransit 8.5.10 + Azure Service Bus transport, `Azure.Messaging.ServiceBus` 7.20.2, Vue 3 + vue-router, xUnit v3 + `AlmostServiceBus.TestHost` 0.6.0 for integration tests.

**Spec:** `docs/superpowers/specs/2026-09-15-admin-error-queues-design.md`

## Global Constraints

- Reuse the existing `ASB_CONNECTIONSTRING` config value — no new secret/config surface.
- `topic` is always passed as a query parameter, never a path segment (fault topic names contain a literal `/`).
- Every retry/discard scan is bounded to one pass over the subscription's message count at call time — never an unbounded loop.
- Purge is destructive; the UI must confirm before calling it.
- Every new `/admin/errors/**` route lives behind the existing `RequireAuthorization("IsAdmin")` group in `WebAppExtensions.cs` — do not add a separate auth check.
- No dead-letter-subqueue routing, TTL override, or MassTransit fault-pipeline change — faulted messages still expire after the existing 2-hour `DefaultMessageTimeToLive`, same as every other queue/topic on this bus.

---

## Task 1: Test infrastructure — ASB-backed fixture and a controllable faulting consumer

**Files:**
- Modify: `src/FplBot.Tests/FplBot.Tests.csproj` — add `AlmostServiceBus.TestHost` 0.6.0
- Create: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs`
- Test: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixtureTests.cs`

**Interfaces:**
- Produces: `AdminErrorQueueFixture` (class, `IAsyncLifetime`) with `Publisher` (`IPublishEndpoint`), `AdminClient` (`ServiceBusAdministrationClient`), `BusClient` (`ServiceBusClient`) properties, and a `DrainMatchingAsync(string topic, string subscription, string bodyContains, int maxMessages = 50)` test-cleanup helper, used by every later task's tests via `[Collection("AdminErrorQueue")]`. Every test that publishes a `PoisonTestMessage` shares one fault topic with every other test in the collection (same message type) — always drain your own message via this helper (or via a real discard/retry/purge call) before the test ends, and always identify "your" message by its `key`/body content, never by raw queue length (which reflects everything currently in the shared topic, not just this test's own message).
- Produces: `PoisonTestMessage(string Key, bool AlwaysFault)` record and `AlwaysFaultsHandler` (`IConsumer<PoisonTestMessage>`) — faults on a message's first delivery (or every delivery if `AlwaysFault` is true), succeeds on the second delivery of the same `Key` otherwise. Later tasks publish this message type to produce controllable faults.
- Produces: `AdminErrorQueueFixtureTests.WaitForMessageAsync(AdminErrorQueueFixture fixture, string bodyContains, int attempts = 20)` (static, internal) — polls every fault topic/subscription by peeking for a message whose body contains `bodyContains` (normally the test's own `key`), returning the `(topic, subscription)` it was found on, or `(null, null)` if it never showed up. Later tasks use this instead of writing their own polling loop, and instead of trusting `ActiveMessageCount` (see Testing note below).

- [ ] **Step 1: Add the test-only package reference**

Edit `src/FplBot.Tests/FplBot.Tests.csproj`, inside the existing `<ItemGroup>` of `PackageReference`s, add:

```xml
<PackageReference Include="AlmostServiceBus.TestHost" Version="0.6.0" />
```

- [ ] **Step 2: Write the fixture, poison consumer, and collection definition**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs`:

```csharp
using System.Collections.Concurrent;
using AlmostServiceBus.TestHost;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using FplBot.WebApi.Admin;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E.Admin;

public class AdminErrorQueueFixture : IAsyncLifetime
{
    // Fixed and distinct from devenv's port 6000 (src/devenv.sh / FplBot.AppHost), so running
    // `dotnet test` alongside a locally running devenv never collides. ServiceBusAdministrationClient
    // only works against a fixed public port for this emulator (its management multiplexer on port
    // 5300 is only bound when a fixed port is requested), so every test needing admin/management
    // operations must share this one fixture instance — see AdminErrorQueueCollection below.
    private const int EmulatorPort = 16712;

    private readonly ServiceBusEmulatorFixture _emulator = new(EmulatorPort);
    private IHost _host = null!;

    public AdminErrorQueueService Service => _host.Services.GetRequiredService<AdminErrorQueueService>();
    public ServiceBusAdministrationClient AdminClient => _host.Services.GetRequiredService<ServiceBusAdministrationClient>();
    public ServiceBusClient BusClient => _host.Services.GetRequiredService<ServiceBusClient>();
    public IPublishEndpoint Publisher => _host.Services.GetRequiredService<IPublishEndpoint>();

    // Every test in this collection publishes the SAME PoisonTestMessage type, so they all share
    // ONE fault topic (MassTransit provisions one fault topic per message TYPE, not per test).
    // Tests must drain their own message when done so the next test doesn't see stray leftovers.
    // Used only by tests — not a production code path, so it's fine to live on the fixture itself.
    public async Task DrainMatchingAsync(string topic, string subscription, string bodyContains, int maxMessages = 50)
    {
        await using var receiver = BusClient.CreateReceiver(topic, subscription);
        var received = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(2));
        foreach (var m in received)
        {
            if (m.Body.ToString().Contains(bodyContains, StringComparison.Ordinal))
                await receiver.CompleteMessageAsync(m);
            else
                await receiver.AbandonMessageAsync(m);
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _emulator.StartAsync();

        // MassTransit 8.5.10 has no ServiceProvider.StartMassTransitAsync()/StopMassTransitAsync()
        // extension on a bare ServiceCollection — its hosted service needs a real IHost to run
        // under, so build one instead of a plain ServiceProvider.
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(b => b.AddConsole());
                services.AddSingleton(new ServiceBusAdministrationClient(_emulator.ConnectionString));
                services.AddSingleton(new ServiceBusClient(_emulator.ConnectionString));
                services.AddSingleton<AdminErrorQueueService>();
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<AlwaysFaultsHandler>();
                    // Matches Hosting/FplBotApplication.cs's production bus config exactly —
                    // verified this has zero effect on the fault topic itself (only on whether a
                    // {queue}_error queue also gets created), but matching it keeps this fixture
                    // faithful to the real topology.
                    x.AddConfigureEndpointsCallback((_, cfg) => cfg.DiscardFaultedMessages());
                    x.UsingAzureServiceBus((ctx, cfg) =>
                    {
                        cfg.Host(_emulator.ConnectionString);
                        cfg.ConfigureEndpoints(ctx);
                    });
                });
            });

        _host = hostBuilder.Build();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await _emulator.DisposeAsync();
    }
}

public record PoisonTestMessage(string Key, bool AlwaysFault);

public class AlwaysFaultsHandler : IConsumer<PoisonTestMessage>
{
    public static readonly ConcurrentDictionary<string, int> Attempts = new();

    public Task Consume(ConsumeContext<PoisonTestMessage> context)
    {
        var attempt = Attempts.AddOrUpdate(context.Message.Key, 1, (_, n) => n + 1);
        if (context.Message.AlwaysFault || attempt == 1)
            throw new InvalidOperationException($"Poison message faulted (attempt {attempt})");

        return Task.CompletedTask;
    }
}

[CollectionDefinition("AdminErrorQueue")]
public class AdminErrorQueueCollection : ICollectionFixture<AdminErrorQueueFixture>;
```

This won't compile yet — `FplBot.WebApi.Admin.AdminErrorQueueService` doesn't exist. That's expected; it's created in Task 2. For now, comment out the `Service` property and the `services.AddSingleton<AdminErrorQueueService>();` line so the rest of the fixture compiles:

```csharp
// public AdminErrorQueueService Service => _host.Services.GetRequiredService<AdminErrorQueueService>();
```

and remove the `services.AddSingleton<AdminErrorQueueService>();` line and its `using FplBot.WebApi.Admin;`. (Task 2 restores both.)

- [ ] **Step 3: Write the test proving the harness itself**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixtureTests.cs`:

```csharp
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
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorQueueFixtureTests" --no-incremental`
Expected: PASS — proves the emulator, the poison consumer, and the fault-topic mechanism work end-to-end before any production code is written.

- [ ] **Step 5: Commit**

```bash
git add src/FplBot.Tests/FplBot.Tests.csproj src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixtureTests.cs
git commit -m "Add ASB-backed test fixture and poison consumer for error-queue tests"
```

---

## Task 2: `AdminErrorQueueService` — list queues and peek messages

**Files:**
- Create: `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs`
- Modify: `src/FplBot/FplBot.csproj` — add `Azure.Messaging.ServiceBus` 7.20.2
- Modify: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs` — restore the `AdminErrorQueueService` registration commented out in Task 1
- Test: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceListTests.cs`

**Interfaces:**
- Consumes: `AdminErrorQueueFixture.Publisher`, `.AdminClient` (from Task 1).
- Produces: `ErrorQueueSummary(string Topic, string Subscription, string MessageType, long Length)`, `FaultException(string ExceptionType, string Message)`, `ErrorQueueMessage(string MessageId, DateTimeOffset EnqueuedTime, string SourceAddress, IReadOnlyList<string> FaultMessageTypes, IReadOnlyList<FaultException> Exceptions, string? OriginalMessageJson)` records, and `AdminErrorQueueService.ListQueuesAsync(CancellationToken)` / `.PeekMessagesAsync(string topic, string subscription, int maxMessages = 50, CancellationToken)` methods — used by Task 3, 4, and 5. `MessageType` is parsed from the topic name (see the spec's "Ground truth" section — the subscription name does not identify anything meaningful; grouping and display both key off the message type instead).

- [ ] **Step 1: Add the `Azure.Messaging.ServiceBus` package reference**

Edit `src/FplBot/FplBot.csproj`, in the `<!-- Messaging -->` item group (alongside `MassTransit.Azure.ServiceBus.Core`), add:

```xml
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.20.2" />
```

- [ ] **Step 2: Write the failing test**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceListTests.cs`:

```csharp
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
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorQueueServiceListTests" --no-incremental`
Expected: FAIL (compile error) — `AdminErrorQueueService`/`ErrorQueueSummary`/`ErrorQueueMessage` don't exist yet.

- [ ] **Step 4: Implement `AdminErrorQueueService` (list + peek)**

Create `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs`:

```csharp
using System.Text.Json.Nodes;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace FplBot.WebApi.Admin;

public record ErrorQueueSummary(string Topic, string Subscription, string MessageType, long Length);

public record FaultException(string ExceptionType, string Message);

public record ErrorQueueMessage(
    string MessageId,
    DateTimeOffset EnqueuedTime,
    string SourceAddress,
    IReadOnlyList<string> FaultMessageTypes,
    IReadOnlyList<FaultException> Exceptions,
    string? OriginalMessageJson);

public class AdminErrorQueueService(ServiceBusAdministrationClient adminClient, ServiceBusClient client)
{
    private const string FaultTopicPrefix = "MassTransit/Fault--";
    private const string FaultTopicSuffix = "--";

    public async Task<IReadOnlyList<ErrorQueueSummary>> ListQueuesAsync(CancellationToken ct = default)
    {
        var result = new List<ErrorQueueSummary>();
        await foreach (var topic in adminClient.GetTopicsAsync(ct))
        {
            if (!topic.Name.StartsWith(FaultTopicPrefix, StringComparison.Ordinal))
                continue;

            var messageType = ParseMessageType(topic.Name);
            await foreach (var sub in adminClient.GetSubscriptionsAsync(topic.Name, ct))
            {
                var runtime = await adminClient.GetSubscriptionRuntimePropertiesAsync(topic.Name, sub.SubscriptionName, ct);
                result.Add(new ErrorQueueSummary(topic.Name, sub.SubscriptionName, messageType, runtime.Value.ActiveMessageCount));
            }
        }
        return result;
    }

    // "MassTransit/Fault--FplBot.Messaging.Contracts.Events.v1/AppInstalled--" -> "FplBot.Messaging.Contracts.Events.v1.AppInstalled"
    // The subscription name carries no consumer identity here (see the spec's "Ground truth"
    // section) — the message type parsed from the topic is the only meaningful label to group and
    // display by.
    private static string ParseMessageType(string topicName)
    {
        var trimmed = topicName[FaultTopicPrefix.Length..];
        if (trimmed.EndsWith(FaultTopicSuffix, StringComparison.Ordinal))
            trimmed = trimmed[..^FaultTopicSuffix.Length];
        return trimmed.Replace('/', '.');
    }

    public async Task<IReadOnlyList<ErrorQueueMessage>> PeekMessagesAsync(
        string topic, string subscription, int maxMessages = 50, CancellationToken ct = default)
    {
        await using var receiver = client.CreateReceiver(topic, subscription);
        var peeked = await receiver.PeekMessagesAsync(maxMessages, cancellationToken: ct);
        return peeked.Select(ToErrorQueueMessage).ToList();
    }

    private static ErrorQueueMessage ToErrorQueueMessage(ServiceBusReceivedMessage message)
    {
        var envelope = JsonNode.Parse(message.Body.ToString())!;
        var fault = envelope["message"]!;

        var exceptions = fault["exceptions"]?.AsArray()
            .Where(e => e is not null)
            .Select(e => new FaultException(
                e!["exceptionType"]?.GetValue<string>() ?? "Unknown",
                e["message"]?.GetValue<string>() ?? ""))
            .ToList() ?? [];

        var faultMessageTypes = fault["faultMessageTypes"]?.AsArray()
            .Where(t => t is not null)
            .Select(t => t!.GetValue<string>())
            .ToList() ?? [];

        var original = fault["message"];

        return new ErrorQueueMessage(
            message.MessageId,
            message.EnqueuedTime,
            envelope["sourceAddress"]?.GetValue<string>() ?? "",
            faultMessageTypes,
            exceptions,
            original?.ToJsonString());
    }
}
```

- [ ] **Step 5: Restore the fixture's `AdminErrorQueueService` registration**

Edit `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs`: uncomment `public AdminErrorQueueService Service => ...`, add back `services.AddSingleton<AdminErrorQueueService>();` in `InitializeAsync`, and add back `using FplBot.WebApi.Admin;`.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorQueueServiceListTests|FullyQualifiedName~AdminErrorQueueFixtureTests" --no-incremental`
Expected: PASS (all 3 tests)

- [ ] **Step 7: Commit**

```bash
git add src/FplBot/FplBot.csproj src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceListTests.cs
git commit -m "Add AdminErrorQueueService.ListQueuesAsync and PeekMessagesAsync"
```

---

## Task 3: `AdminErrorQueueService` — retry and discard a single message

**Files:**
- Modify: `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs`
- Test: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceRetryDiscardTests.cs`

**Interfaces:**
- Consumes: `AdminErrorQueueFixture.Publisher`, `.Service`, `AlwaysFaultsHandler.Attempts` (from Task 1/2).
- Produces: `AdminErrorQueueService.RetryMessageAsync(string topic, string subscription, string messageId, CancellationToken)` and `.DiscardMessageAsync(string topic, string subscription, string messageId, CancellationToken)`, both returning `Task<bool>` (`true` if a matching message was found and acted on) — used by Task 5.

- [ ] **Step 1: Write the failing tests**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceRetryDiscardTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorQueueServiceRetryDiscardTests" --no-incremental`
Expected: FAIL (compile error) — `RetryMessageAsync`/`DiscardMessageAsync` don't exist yet.

- [ ] **Step 3: Implement retry and discard**

Add to `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs` (inside the `AdminErrorQueueService` class, after `PeekMessagesAsync`):

```csharp
    public Task<bool> RetryMessageAsync(string topic, string subscription, string messageId, CancellationToken ct = default) =>
        ScanAndActAsync(topic, subscription, messageId, async (receiver, message) =>
        {
            var envelope = JsonNode.Parse(message.Body.ToString())!;
            var fault = envelope["message"]!;
            var messageType = fault["faultMessageTypes"]!.AsArray()[0]!.GetValue<string>();
            var sourceAddress = envelope["sourceAddress"]!.GetValue<string>();
            var targetQueue = sourceAddress.Split('/').Last();
            var original = fault["message"];

            // urn:message:FplBot.Messaging.Contracts.Events.v1:AppInstalled -> FplBot.Messaging.Contracts.Events.v1/AppInstalled
            var typePath = messageType.Replace("urn:message:", "");
            var lastColon = typePath.LastIndexOf(':');
            typePath = typePath[..lastColon] + "/" + typePath[(lastColon + 1)..];

            var retryEnvelope = new JsonObject
            {
                ["messageId"] = Guid.NewGuid().ToString(),
                ["requestId"] = null,
                ["correlationId"] = null,
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["initiatorId"] = null,
                ["sourceAddress"] = "sb://localhost/AdminErrorQueueService",
                ["destinationAddress"] = $"sb://localhost/{typePath}?type=topic",
                ["responseAddress"] = null,
                ["faultAddress"] = null,
                ["messageType"] = new JsonArray(messageType),
                ["message"] = original?.DeepClone(),
                ["expirationTime"] = null,
                ["sentTime"] = DateTimeOffset.UtcNow,
                ["headers"] = new JsonObject(),
            };

            var sender = client.CreateSender(targetQueue);
            await sender.SendMessageAsync(new ServiceBusMessage(retryEnvelope.ToJsonString())
            {
                ContentType = "application/vnd.masstransit+json",
            }, ct);

            await receiver.CompleteMessageAsync(message, ct);
        }, ct);

    public Task<bool> DiscardMessageAsync(string topic, string subscription, string messageId, CancellationToken ct = default) =>
        ScanAndActAsync(topic, subscription, messageId, (receiver, message) => receiver.CompleteMessageAsync(message, ct), ct);

    // There is no fetch-by-message-id in Azure Service Bus, so this scans the subscription
    // (PeekLock), abandoning every non-matching message immediately so it returns to the
    // subscription, and stops after one full pass (bounded by the subscription's message
    // count at call time) so a stale/missing id can't loop forever.
    private async Task<bool> ScanAndActAsync(
        string topic, string subscription, string messageId,
        Func<ServiceBusReceiver, ServiceBusReceivedMessage, Task> onMatch, CancellationToken ct)
    {
        await using var receiver = client.CreateReceiver(topic, subscription);
        var runtime = await adminClient.GetSubscriptionRuntimePropertiesAsync(topic, subscription, ct);
        var scanLimit = (int)runtime.Value.ActiveMessageCount;

        for (var i = 0; i < scanLimit; i++)
        {
            var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), ct);
            if (message is null)
                break;

            if (message.MessageId != messageId)
            {
                await receiver.AbandonMessageAsync(message, cancellationToken: ct);
                continue;
            }

            await onMatch(receiver, message);
            return true;
        }

        return false;
    }
```

No new `using` directives are needed — `Azure.Messaging.ServiceBus` and `System.Text.Json.Nodes` are already imported at the top of the file from Task 2.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorQueueServiceRetryDiscardTests" --no-incremental`
Expected: PASS (all 3 tests)

- [ ] **Step 5: Commit**

```bash
git add src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceRetryDiscardTests.cs
git commit -m "Add AdminErrorQueueService.RetryMessageAsync and DiscardMessageAsync"
```

---

## Task 4: `AdminErrorQueueService` — purge an entire queue

**Files:**
- Modify: `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs`
- Test: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServicePurgeTests.cs`

**Interfaces:**
- Consumes: `AdminErrorQueueFixture` (from Task 1/2).
- Produces: `AdminErrorQueueService.PurgeQueueAsync(string topic, string subscription, CancellationToken)` returning `Task<int>` (count purged) — used by Task 5.

- [ ] **Step 1: Write the failing test**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServicePurgeTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorQueueServicePurgeTests" --no-incremental`
Expected: FAIL (compile error) — `PurgeQueueAsync` doesn't exist yet.

- [ ] **Step 3: Implement purge**

Add to `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs` (inside the class, after `ScanAndActAsync`):

```csharp
    public async Task<int> PurgeQueueAsync(string topic, string subscription, CancellationToken ct = default)
    {
        await using var receiver = client.CreateReceiver(topic, subscription);
        var runtime = await adminClient.GetSubscriptionRuntimePropertiesAsync(topic, subscription, ct);
        var bound = (int)runtime.Value.ActiveMessageCount;
        var purged = 0;

        while (purged < bound)
        {
            var messages = await receiver.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromSeconds(5), cancellationToken: ct);
            if (messages.Count == 0)
                break;

            foreach (var message in messages)
            {
                await receiver.CompleteMessageAsync(message, ct);
                purged++;
            }
        }

        return purged;
    }
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorQueueServicePurgeTests" --no-incremental`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs src/FplBot.Tests/E2E/Admin/AdminErrorQueueServicePurgeTests.cs
git commit -m "Add AdminErrorQueueService.PurgeQueueAsync"
```

---

## Task 5: Admin API endpoints

**Files:**
- Create: `src/FplBot/Services/WebApi/Endpoints/Api/Admin/AdminErrorEndpoints.cs`
- Modify: `src/FplBot/Services/WebApi/Infrastructure/WebApplicationBuilderExtensions.cs` — register `ServiceBusAdministrationClient`, `ServiceBusClient`, `AdminErrorQueueService`
- Modify: `src/FplBot/Services/WebApi/Infrastructure/WebAppExtensions.cs` — map `AdminErrorEndpoints`
- Test: `src/FplBot.Tests/E2E/Admin/AdminErrorEndpointsTests.cs`

**Interfaces:**
- Consumes: `AdminErrorQueueService` (from Tasks 2–4), `AdminErrorQueueFixture.Service` (from Task 1).
- Produces: `GET /api/admin/errors/queues`, `GET /api/admin/errors/queue/messages`, `POST /api/admin/errors/queue/messages/{messageId}/retry`, `POST /api/admin/errors/queue/messages/{messageId}/discard`, `POST /api/admin/errors/queue/purge` — consumed by the frontend in Tasks 6–8.

- [ ] **Step 1: Write the failing tests**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorEndpointsTests.cs`:

```csharp
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FplBot.Tests.E2E.Admin;

// Same rule as every earlier task's tests: identify this test's own message by its `key`, never
// by raw Length/ActiveMessageCount, since the fault topic is shared across the whole collection.
[Collection("AdminErrorQueue")]
public class AdminErrorEndpointsTests(AdminErrorQueueFixture fixture)
{
    [Fact]
    public async Task GetQueues_ReturnsOkWithTheFaultedQueue()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var result = await AdminErrorEndpoints.GetQueues(fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ErrorQueueSummary>>>(result);
        Assert.Contains(ok.Value!, q => q.Topic == topic && q.Subscription == subscription);

        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }

    [Fact]
    public async Task RetryMessage_UnknownId_ReturnsNotFound()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var result = await AdminErrorEndpoints.RetryMessage(
            Guid.NewGuid().ToString(), topic!, subscription!, fixture.Service, CancellationToken.None);

        Assert.IsType<NotFound>(result);

        await fixture.DrainMatchingAsync(topic!, subscription!, key);
    }

    [Fact]
    public async Task PurgeQueue_ReturnsPurgedCount()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        var (topic, subscription) = await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, key);
        Assert.NotNull(topic);

        var result = await AdminErrorEndpoints.PurgeQueue(topic!, subscription!, fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<PurgeResult>>(result);
        Assert.True(ok.Value!.Purged >= 1);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorEndpointsTests" --no-incremental`
Expected: FAIL (compile error) — `AdminErrorEndpoints` doesn't exist yet.

- [ ] **Step 3: Implement the endpoints**

Create `src/FplBot/Services/WebApi/Endpoints/Api/Admin/AdminErrorEndpoints.cs`:

```csharp
using FplBot.WebApi.Admin;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record PurgeResult(int Purged);

public static class AdminErrorEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/errors/queues", GetQueues);
        group.MapGet("/errors/queue/messages", GetMessages);
        group.MapPost("/errors/queue/messages/{messageId}/retry", RetryMessage);
        group.MapPost("/errors/queue/messages/{messageId}/discard", DiscardMessage);
        group.MapPost("/errors/queue/purge", PurgeQueue);
    }

    internal static async Task<IResult> GetQueues(AdminErrorQueueService service, CancellationToken ct)
        => TypedResults.Ok(await service.ListQueuesAsync(ct));

    internal static async Task<IResult> GetMessages(string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
        => TypedResults.Ok(await service.PeekMessagesAsync(topic, subscription, ct: ct));

    internal static async Task<IResult> RetryMessage(string messageId, string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
    {
        var retried = await service.RetryMessageAsync(topic, subscription, messageId, ct);
        return retried ? TypedResults.Ok(new { message = "Message retried" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> DiscardMessage(string messageId, string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
    {
        var discarded = await service.DiscardMessageAsync(topic, subscription, messageId, ct);
        return discarded ? TypedResults.Ok(new { message = "Message discarded" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> PurgeQueue(string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
    {
        var purged = await service.PurgeQueueAsync(topic, subscription, ct);
        return TypedResults.Ok(new PurgeResult(purged));
    }
}
```

- [ ] **Step 4: Register the service and map the endpoints**

Edit `src/FplBot/Services/WebApi/Infrastructure/WebApplicationBuilderExtensions.cs`. Add `using Azure.Messaging.ServiceBus;`, `using Azure.Messaging.ServiceBus.Administration;`, and `using FplBot.WebApi.Admin;` to the top, and add this block right after `services.AddIndexingServices(configuration, redisConn);` (line 76):

```csharp
        var asbConnectionString = configuration["ASB_CONNECTIONSTRING"]
            ?? throw new InvalidOperationException("Service bus connection string not configured. Set ASB_CONNECTIONSTRING.");
        services.AddSingleton(new ServiceBusAdministrationClient(asbConnectionString));
        services.AddSingleton(new ServiceBusClient(asbConnectionString));
        services.AddSingleton<AdminErrorQueueService>();
```

Edit `src/FplBot/Services/WebApi/Infrastructure/WebAppExtensions.cs`: add `using FplBot.WebApi.Endpoints.Api.Admin;` is already present (the file already imports that namespace for the other `Admin*Endpoints`); add this line after `AdminHealthEndpoints.Map(admin, env);` (line 74):

```csharp
        AdminErrorEndpoints.Map(admin);
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test src/FplBot.Tests --filter "FullyQualifiedName~AdminErrorEndpointsTests" --no-incremental`
Expected: PASS (all 3 tests)

- [ ] **Step 6: Full build check**

Run: `dotnet build src/FplBot.slnx --no-incremental`
Expected: builds with no warnings — confirms `WebApiService` still wires correctly end-to-end (the `ASB_CONNECTIONSTRING`-backed clients are constructed lazily, so this doesn't require a live Service Bus).

- [ ] **Step 7: Commit**

```bash
git add src/FplBot/Services/WebApi/Endpoints/Api/Admin/AdminErrorEndpoints.cs src/FplBot/Services/WebApi/Infrastructure/WebApplicationBuilderExtensions.cs src/FplBot/Services/WebApi/Infrastructure/WebAppExtensions.cs src/FplBot.Tests/E2E/Admin/AdminErrorEndpointsTests.cs
git commit -m "Add /api/admin/errors/** endpoints for the error-queues admin feature"
```

---

## Task 6: Frontend — types and API client functions

**Files:**
- Modify: `src/FplBot/Services/WebApi/ClientApp/src/api/types.ts`
- Modify: `src/FplBot/Services/WebApi/ClientApp/src/api/api.ts`

**Interfaces:**
- Produces: TS interfaces `ErrorQueueSummary`, `FaultException`, `ErrorQueueMessage` and functions `getErrorQueues()`, `getErrorQueueMessages(topic, subscription)`, `retryErrorMessage(topic, subscription, messageId)`, `discardErrorMessage(topic, subscription, messageId)`, `purgeErrorQueue(topic, subscription)` — used by Tasks 7 and 8.

- [ ] **Step 1: Add the types**

Edit `src/FplBot/Services/WebApi/ClientApp/src/api/types.ts`, appending at the end of the file:

```typescript
// ---- Admin: error queues ----

export interface ErrorQueueSummary {
  topic: string;
  subscription: string;
  messageType: string;
  length: number;
}

export interface FaultException {
  exceptionType: string;
  message: string;
}

export interface ErrorQueueMessage {
  messageId: string;
  enqueuedTime: string;
  sourceAddress: string;
  faultMessageTypes: string[];
  exceptions: FaultException[];
  originalMessageJson: string | null;
}
```

- [ ] **Step 2: Add the API functions**

Edit `src/FplBot/Services/WebApi/ClientApp/src/api/api.ts`: add `ErrorQueueMessage` and `ErrorQueueSummary` to the `import type { ... } from "./types"` block at the top, and append this section at the end of the file:

```typescript
// ---- Admin: error queues ----

export function getErrorQueues(): Promise<ErrorQueueSummary[]> {
  return request("/api/admin/errors/queues");
}

export function getErrorQueueMessages(topic: string, subscription: string): Promise<ErrorQueueMessage[]> {
  const params = new URLSearchParams({ topic, subscription });
  return request(`/api/admin/errors/queue/messages?${params.toString()}`);
}

export function retryErrorMessage(topic: string, subscription: string, messageId: string): Promise<MessageResponse> {
  const params = new URLSearchParams({ topic, subscription });
  return postJson(`/api/admin/errors/queue/messages/${encodeURIComponent(messageId)}/retry?${params.toString()}`);
}

export function discardErrorMessage(topic: string, subscription: string, messageId: string): Promise<MessageResponse> {
  const params = new URLSearchParams({ topic, subscription });
  return postJson(`/api/admin/errors/queue/messages/${encodeURIComponent(messageId)}/discard?${params.toString()}`);
}

export function purgeErrorQueue(topic: string, subscription: string): Promise<{ purged: number }> {
  const params = new URLSearchParams({ topic, subscription });
  return postJson(`/api/admin/errors/queue/purge?${params.toString()}`);
}
```

- [ ] **Step 3: Typecheck**

Run: `cd src/FplBot/Services/WebApi/ClientApp && npm run typecheck`
Expected: no errors

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/WebApi/ClientApp/src/api/types.ts src/FplBot/Services/WebApi/ClientApp/src/api/api.ts
git commit -m "Add frontend types and API client functions for error queues"
```

---

## Task 7: Frontend — error queues list view, section wrapper, and navigation

**Files:**
- Create: `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorsSection.vue`
- Create: `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueuesView.vue`
- Modify: `src/FplBot/Services/WebApi/ClientApp/src/router.ts`
- Modify: `src/FplBot/Services/WebApi/ClientApp/src/layouts/AdminLayout.vue`

**Interfaces:**
- Consumes: `getErrorQueues()`, `purgeErrorQueue()`, `describeAdminError()` (from Task 6 and `useAdminAuth.ts`).
- Produces: route `admin-errors-queues` at `/admin/errors`, and the router push target `{ path: "/admin/errors/queue", query: { topic, subscription, messageType } }` that Task 8's detail view is reached from.

- [ ] **Step 1: Create the section wrapper**

Create `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorsSection.vue`:

```vue
<template>
  <router-view />
</template>
```

- [ ] **Step 2: Create the list view**

Create `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueuesView.vue`:

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getErrorQueues, purgeErrorQueue } from "../../api/api";
import type { ErrorQueueSummary } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const queues = ref<ErrorQueueSummary[]>([]);
const loading = ref(true);
const error = ref("");
const purging = ref<string | null>(null);

function queueKey(q: ErrorQueueSummary) {
  return `${q.topic}::${q.subscription}`;
}

async function load() {
  loading.value = true;
  error.value = "";
  try {
    queues.value = await getErrorQueues();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

async function purge(queue: ErrorQueueSummary) {
  if (!confirm(`Purge all ${queue.length} message(s) for "${queue.messageType}"? This cannot be undone.`)) return;
  purging.value = queueKey(queue);
  error.value = "";
  try {
    await purgeErrorQueue(queue.topic, queue.subscription);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    purging.value = null;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <h1>Error queues</h1>
    <p class="lead">Faulted messages, grouped by message type. Open a queue to see which consumer actually faulted per message.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <p v-if="queues.length === 0">No error queues — nothing has faulted.</p>
        <table v-else class="admin-table">
          <thead>
            <tr>
              <th>Message type</th>
              <th>Length</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="q in queues" :key="queueKey(q)">
              <td>{{ q.messageType }}</td>
              <td>{{ q.length }}</td>
              <td class="row-actions">
                <router-link
                  class="btn small btn-secondary"
                  :to="{ path: '/admin/errors/queue', query: { topic: q.topic, subscription: q.subscription, messageType: q.messageType } }"
                >
                  View
                </router-link>
                <button
                  class="btn small danger"
                  :disabled="purging === queueKey(q)"
                  @click="purge(q)"
                >
                  {{ purging === queueKey(q) ? "Purging..." : "Purge" }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </template>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.row-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-secondary {
  background: #f3f4f6;
  color: inherit;
  border: 1px solid #d1d5db;
}

.btn-secondary:hover {
  background: #e5e7eb;
  color: inherit;
}
</style>
```

- [ ] **Step 3: Wire the routes**

Edit `src/FplBot/Services/WebApi/ClientApp/src/router.ts`. Add this block to the `/admin` route's `children` array, right after the `search` block (after line 101's closing `},`):

```typescript
        {
          path: "errors",
          component: () => import("./views/admin/ErrorsSection.vue"),
          children: [
            {
              path: "",
              name: "admin-errors-queues",
              component: () => import("./views/admin/ErrorQueuesView.vue"),
            },
            {
              path: "queue",
              name: "admin-errors-queue-detail",
              component: () => import("./views/admin/ErrorQueueDetailView.vue"),
              props: (route) => ({ topic: route.query.topic, subscription: route.query.subscription, messageType: route.query.messageType }),
            },
          ],
        },
```

(`ErrorQueueDetailView.vue` is created in Task 8 — the dynamic `import()` means this compiles fine before that file exists, but visiting the detail route would 404 in dev until then. That's acceptable since Task 8 follows immediately after.)

- [ ] **Step 4: Add the nav link**

Edit `src/FplBot/Services/WebApi/ClientApp/src/layouts/AdminLayout.vue`, update the `navLinks` array (line 23-27):

```typescript
const navLinks = [
  { to: "/admin/slack", label: "Slack" },
  { to: "/admin/discord", label: "Discord" },
  { to: "/admin/search", label: "Search" },
  { to: "/admin/errors", label: "Errors" },
];
```

- [ ] **Step 5: Typecheck**

Run: `cd src/FplBot/Services/WebApi/ClientApp && npm run typecheck`
Expected: no errors

- [ ] **Step 6: Commit**

```bash
git add src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorsSection.vue src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueuesView.vue src/FplBot/Services/WebApi/ClientApp/src/router.ts src/FplBot/Services/WebApi/ClientApp/src/layouts/AdminLayout.vue
git commit -m "Add Errors tab list view, purge action, and navigation"
```

---

## Task 8: Frontend — error queue detail view (messages, body, retry/discard)

**Files:**
- Create: `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue`

**Interfaces:**
- Consumes: `getErrorQueueMessages()`, `retryErrorMessage()`, `discardErrorMessage()` (from Task 6), route props `topic`/`subscription`/`messageType` (wired in Task 7).

- [ ] **Step 1: Create the detail view**

Create `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue`:

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getErrorQueueMessages, retryErrorMessage, discardErrorMessage } from "../../api/api";
import type { ErrorQueueMessage } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const props = defineProps<{ topic: string; subscription: string; messageType: string }>();

const messages = ref<ErrorQueueMessage[]>([]);
const loading = ref(true);
const error = ref("");
const acting = ref<string | null>(null);
const expanded = ref<Set<string>>(new Set());

async function load() {
  loading.value = true;
  error.value = "";
  try {
    messages.value = await getErrorQueueMessages(props.topic, props.subscription);
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

function toggle(messageId: string) {
  if (expanded.value.has(messageId)) {
    expanded.value.delete(messageId);
  } else {
    expanded.value.add(messageId);
  }
}

function prettyBody(m: ErrorQueueMessage): string {
  if (!m.originalMessageJson) return "(empty body)";
  try {
    return JSON.stringify(JSON.parse(m.originalMessageJson), null, 2);
  } catch {
    return m.originalMessageJson;
  }
}

async function retry(m: ErrorQueueMessage) {
  acting.value = m.messageId;
  error.value = "";
  try {
    await retryErrorMessage(props.topic, props.subscription, m.messageId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    acting.value = null;
  }
}

async function discard(m: ErrorQueueMessage) {
  if (!confirm("Discard this message? This cannot be undone.")) return;
  acting.value = m.messageId;
  error.value = "";
  try {
    await discardErrorMessage(props.topic, props.subscription, m.messageId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    acting.value = null;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <router-link to="/admin/errors" class="btn small btn-secondary">&larr; Back to queues</router-link>
    <h1>{{ messageType }}</h1>
    <p class="lead">{{ messages.length }} message(s) in this queue. Check each message's source address below for which consumer actually faulted.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <p v-if="messages.length === 0">No messages.</p>
        <div v-for="m in messages" :key="m.messageId" class="message">
          <div class="message-header">
            <div>
              <div><b>{{ m.messageId }}</b></div>
              <div class="meta">{{ new Date(m.enqueuedTime).toLocaleString() }} &middot; {{ m.sourceAddress }}</div>
            </div>
            <div class="message-actions">
              <button class="btn small" :disabled="acting === m.messageId" @click="retry(m)">
                {{ acting === m.messageId ? "Retrying..." : "Retry" }}
              </button>
              <button class="btn small danger" :disabled="acting === m.messageId" @click="discard(m)">
                {{ acting === m.messageId ? "Discarding..." : "Discard" }}
              </button>
            </div>
          </div>

          <ul class="exceptions">
            <li v-for="(ex, i) in m.exceptions" :key="i">
              <b>{{ ex.exceptionType }}</b>: {{ ex.message }}
            </li>
          </ul>

          <button class="btn small btn-secondary" @click="toggle(m.messageId)">
            {{ expanded.has(m.messageId) ? "Hide body" : "Show body" }}
          </button>
          <pre v-if="expanded.has(m.messageId)" class="body">{{ prettyBody(m) }}</pre>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.message {
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  padding: 1rem;
  margin-bottom: 1rem;
  background: #e9ebef;
}

.message-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 0.5rem;
}

.meta {
  color: #6b7280;
  font-size: 0.85rem;
}

.message-actions {
  display: flex;
  gap: 0.5rem;
  flex-shrink: 0;
}

.exceptions {
  font-size: 0.9rem;
  color: #b91c1c;
  margin: 0.5rem 0;
}

.body {
  background: #1f2937;
  color: #e5e7eb;
  padding: 1rem;
  border-radius: 0.375rem;
  overflow-x: auto;
  font-size: 0.85rem;
  margin-top: 0.5rem;
}

.btn-secondary {
  background: #f3f4f6;
  color: inherit;
  border: 1px solid #d1d5db;
}

.btn-secondary:hover {
  background: #e5e7eb;
  color: inherit;
}
</style>
```

- [ ] **Step 2: Typecheck**

Run: `cd src/FplBot/Services/WebApi/ClientApp && npm run typecheck`
Expected: no errors

- [ ] **Step 3: Full solution build**

Run: `dotnet build src/FplBot.slnx --no-incremental`
Expected: builds with no warnings

- [ ] **Step 4: Full test run**

Run: `dotnet test src/FplBot.Tests --no-incremental`
Expected: all tests pass, including every `AdminErrorQueue*`/`AdminErrorEndpoints` test from Tasks 1–5

- [ ] **Step 5: Manual smoke check (dev server)**

With the user's own `devenv.sh`/AppHost running (never start it yourself — see project conventions), open the admin UI, sign in, click the new "Errors" tab, and confirm it loads (likely empty, since nothing has faulted). This is a visual/UX check the automated tests above don't cover — flag to the user if you can't run it yourself.

- [ ] **Step 6: Commit**

```bash
git add src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue
git commit -m "Add error queue detail view with message body inspection, retry, and discard"
```
