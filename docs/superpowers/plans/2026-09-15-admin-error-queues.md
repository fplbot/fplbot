# Admin Error Queues Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an "Errors" tab to the fplbot admin UI that lists MassTransit's standard `{Consumer}_error` queues (one per faulting consumer), lets an admin inspect a faulted message's details and body, retry it, discard it, or purge an entire queue.

**Architecture:** Remove `cfg.DiscardFaultedMessages()` from the production bus config so MassTransit's default behavior applies: a faulted message moves to a plain queue named `{Consumer}_error`, with exception details attached as `MT-Fault-*` message application properties and the original envelope untouched in the body. A new `AdminErrorQueueService` in the WebApi service talks directly to Azure Service Bus via the `Azure.Messaging.ServiceBus` SDK (list/peek/retry/discard/purge against these plain queues — no topics, no subscriptions, no `ForwardTo` risk). New minimal-API endpoints under `/api/admin/errors/**` expose it; a new Vue admin tab consumes those endpoints.

**Tech Stack:** .NET 11 minimal APIs, MassTransit 8.5.10 + Azure Service Bus transport, `Azure.Messaging.ServiceBus` 7.20.2, Vue 3 + vue-router, xUnit v3 + `AlmostServiceBus.TestHost` 0.6.0 for integration tests.

**Spec:** `docs/superpowers/specs/2026-09-15-admin-error-queues-design.md`

## Global Constraints

- Reuse the existing `ASB_CONNECTIONSTRING` config value — no new secret/config surface.
- Every retry/discard/purge/messages call must reject a queue name that doesn't end in `_error` (via `AdminErrorQueueService.IsErrorQueue(string)`) — this is a destructive admin surface and must never reach a live, non-error queue.
- Every retry/discard scan is bounded by tracking already-abandoned message ids and stopping on the first repeat (a full cycle with no match) — never by `ActiveMessageCount`, which reads 0 unconditionally against this repo's test emulator (`AlmostServiceBus.TestHost` 0.6.0, confirmed for both queues and subscriptions by direct testing — its management API never serializes message-count fields at all).
- Purge loops until a receive call returns zero messages — self-terminating (every message is completed, never abandoned back), so it needs no count-based bound either.
- Every new `/admin/errors/**` route lives behind the existing `RequireAuthorization("IsAdmin")` group in `WebAppExtensions.cs` — do not add a separate auth check.
- `DiscardFaultedMessages()` is intentionally removed from `Hosting/FplBotApplication.cs` as part of this plan — this is a real, deliberate messaging-pipeline change (faulted messages now persist in `_error` queues instead of being discarded), not an accident.
- Faulted messages still expire after the existing 2-hour `DefaultMessageTimeToLive`, same as every other queue on this bus — no TTL override, no dead-letter-subqueue routing.

---

## Task 1: Remove DiscardFaultedMessages(), and build the ASB-backed test fixture

**Files:**
- Modify: `src/FplBot/Hosting/FplBotApplication.cs` — remove `cfg.DiscardFaultedMessages()`
- Modify: `src/FplBot.Tests/FplBot.Tests.csproj` — add `AlmostServiceBus.TestHost` 0.6.0
- Create: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs`
- Test: `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixtureTests.cs`

**Interfaces:**
- Produces: `AdminErrorQueueFixture` (class, `IAsyncLifetime`) with `Publisher` (`IPublishEndpoint`), `AdminClient` (`ServiceBusAdministrationClient`), `BusClient` (`ServiceBusClient`) properties, and a `DrainMatchingAsync(string queue, string bodyContains, int maxMessages = 50)` test-cleanup helper, used by every later task's tests via `[Collection("AdminErrorQueue")]`.
- Produces: `PoisonTestMessage(string Key, bool AlwaysFault)` record and `AlwaysFaultsHandler` (`IConsumer<PoisonTestMessage>`) — faults on a message's first delivery (or every delivery if `AlwaysFault` is true), succeeds on the second delivery of the same `Key` otherwise. Its faults land in queue `AlwaysFaultsHandler_error` (MassTransit names the error queue after the consumer, matching this repo's existing queue-naming convention). Later tasks publish this message type to produce controllable faults.
- Produces: `AdminErrorQueueFixtureTests.WaitForMessageAsync(AdminErrorQueueFixture fixture, string queue, string bodyContains, int attempts = 20)` (static, internal) — polls the given queue by peeking for a message whose body contains `bodyContains` (normally the test's own `key`), returning `true`/`false`. Later tasks use this instead of writing their own polling loop, and instead of trusting `ActiveMessageCount`.

- [ ] **Step 1: Remove `DiscardFaultedMessages()` from production**

Edit `src/FplBot/Hosting/FplBotApplication.cs`. Find:

```csharp
        services.AddMassTransit(x =>
        {
            foreach (var svc in active)
                svc.ConfigureMassTransit(x);
            x.AddConfigureEndpointsCallback((_, cfg) => cfg.DiscardFaultedMessages());
            configureBus(x);
        });
```

Replace with:

```csharp
        services.AddMassTransit(x =>
        {
            foreach (var svc in active)
                svc.ConfigureMassTransit(x);
            configureBus(x);
        });
```

- [ ] **Step 2: Run the full existing test suite to confirm nothing depends on discard-on-fault**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental` then `./bin/Debug/net11.0/FplBot.Tests` (full run, no filter).
Expected: same pass/fail counts as before this change (the existing suite uses `AppFixture`'s in-memory MassTransit transport, which doesn't persist `_error` queues across runs the way a real broker does, so this change should be invisible to it — but confirm empirically rather than assume). If you see failures in `FplBot.Tests.E2E.Search.SearchEndpointsTests`/`AdminSearchEndpointsTests` (Elasticsearch Testcontainer errors), that's pre-existing, unrelated environmental flakiness under constrained local Docker memory — not something this step is checking for. Compare against a baseline run on the commit *before* Step 1's edit if you want to confirm that specifically.

- [ ] **Step 3: Add the test-only package reference**

Edit `src/FplBot.Tests/FplBot.Tests.csproj`, inside the existing `<ItemGroup>` of `PackageReference`s, add:

```xml
<PackageReference Include="AlmostServiceBus.TestHost" Version="0.6.0" />
```

- [ ] **Step 4: Write the fixture, poison consumer, and collection definition**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs`:

```csharp
using System.Collections.Concurrent;
using AlmostServiceBus.TestHost;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
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

    // public AdminErrorQueueService Service => _host.Services.GetRequiredService<AdminErrorQueueService>();
    public ServiceBusAdministrationClient AdminClient => _host.Services.GetRequiredService<ServiceBusAdministrationClient>();
    public ServiceBusClient BusClient => _host.Services.GetRequiredService<ServiceBusClient>();
    public IPublishEndpoint Publisher => _host.Services.GetRequiredService<IPublishEndpoint>();

    // Every test in this collection that publishes PoisonTestMessage shares the SAME error queue
    // (AlwaysFaultsHandler_error) — drain your own message via this helper (or via a real
    // discard/retry/purge call) before the test ends, and always identify "your" message by its
    // `key`/body content, never by raw queue length.
    public async Task DrainMatchingAsync(string queue, string bodyContains, int maxMessages = 50)
    {
        await using var receiver = BusClient.CreateReceiver(queue);
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

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(b => b.AddConsole());
                services.AddSingleton(new ServiceBusAdministrationClient(_emulator.ConnectionString));
                services.AddSingleton(new ServiceBusClient(_emulator.ConnectionString));
                // services.AddSingleton<AdminErrorQueueService>();
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<AlwaysFaultsHandler>();
                    // Deliberately NOT calling DiscardFaultedMessages() — matches the corrected
                    // production config (Step 1), so faults land in AlwaysFaultsHandler_error.
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

This won't compile yet — `FplBot.WebApi.Admin.AdminErrorQueueService` doesn't exist. That's expected; it's created in Task 2. The `Service` property and its DI registration are already commented out above so the rest of the fixture compiles. (Task 2 restores both, and adds `using FplBot.WebApi.Admin;`.)

- [ ] **Step 5: Write the test proving the harness itself**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixtureTests.cs`:

```csharp
namespace FplBot.Tests.E2E.Admin;

[Collection("AdminErrorQueue")]
public class AdminErrorQueueFixtureTests(AdminErrorQueueFixture fixture)
{
    public const string ErrorQueueName = "AlwaysFaultsHandler_error";

    [Fact]
    public async Task FaultedMessage_LandsInTheErrorQueue()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

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
            var peeked = await receiver.PeekMessagesAsync(10);
            if (peeked.Any(m => m.Body.ToString().Contains(bodyContains, StringComparison.Ordinal)))
                return true;
            await Task.Delay(250);
        }
        return false;
    }
}
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental` then `./bin/Debug/net11.0/FplBot.Tests --filter-query "/*/*/AdminErrorQueueFixtureTests/*"` (a prior task in this plan's history found `dotnet test --filter` can produce spurious "Zero tests ran / Handshake failures" on this repo's xUnit v3 runner — running the built executable directly with `--filter-query` is reliable).
Expected: PASS — proves the emulator, the corrected (no-discard) bus config, the poison consumer, and the `_error` queue mechanism work end-to-end before any production admin code is written.

- [ ] **Step 7: Commit**

```bash
git add src/FplBot/Hosting/FplBotApplication.cs src/FplBot.Tests/FplBot.Tests.csproj src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixtureTests.cs
git commit -m "Remove DiscardFaultedMessages() and add ASB-backed test fixture for error-queue tests"
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
- Produces: `ErrorQueueSummary(string Queue, string Consumer, long Length)`, `ErrorQueueMessage(string MessageId, DateTimeOffset EnqueuedTime, string ExceptionType, string ExceptionMessage, string? StackTrace, string? ConsumerType, string? OriginalMessageJson)` records; `AdminErrorQueueService.IsErrorQueue(string queue)` (public static, `queue.EndsWith("_error")`) — used by Task 5's endpoints as the destructive-surface guard; `AdminErrorQueueService.ListQueuesAsync(CancellationToken)` / `.PeekMessagesAsync(string queue, int maxMessages = 50, CancellationToken)` methods — used by Task 3, 4, and 5. `Consumer` is the queue name with the `_error` suffix stripped — this **is** the real consumer name, no parsing needed (unlike the discarded topic-based design). `Length` reads `ActiveMessageCount`, falling back to a peek-based count when that's 0 — confirmed unconditionally 0 against `AlmostServiceBus.TestHost` 0.6.0 for queues (same gap already found for subscriptions), not a timing lag.

- [ ] **Step 1: Add the `Azure.Messaging.ServiceBus` package reference**

Edit `src/FplBot/FplBot.csproj`, in the `<!-- Messaging -->` item group (alongside `MassTransit.Azure.ServiceBus.Core`), add:

```xml
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.20.2" />
```

- [ ] **Step 2: Write the failing test**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceListTests.cs`:

```csharp
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
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var found = await AdminErrorQueueFixtureTests.WaitForMessageAsync(
            fixture, AdminErrorQueueFixtureTests.ErrorQueueName, key);
        Assert.True(found);

        var queues = await fixture.Service.ListQueuesAsync();
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
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        var found = await AdminErrorQueueFixtureTests.WaitForMessageAsync(
            fixture, AdminErrorQueueFixtureTests.ErrorQueueName, key);
        Assert.True(found);

        var messages = await fixture.Service.PeekMessagesAsync(AdminErrorQueueFixtureTests.ErrorQueueName);
        var message = messages.Single(m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));

        Assert.Equal("System.InvalidOperationException", message.ExceptionType);
        Assert.Contains("faulted", message.ExceptionMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(nameof(AlwaysFaultsHandler), message.ConsumerType!.Split('.').Last());

        await fixture.DrainMatchingAsync(AdminErrorQueueFixtureTests.ErrorQueueName, key);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental`
Expected: FAIL (compile error) — `AdminErrorQueueService`/`ErrorQueueSummary`/`ErrorQueueMessage`/`fixture.Service` don't exist yet.

- [ ] **Step 4: Implement `AdminErrorQueueService` (list + peek)**

Create `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs`:

```csharp
using System.Text.Json.Nodes;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace FplBot.WebApi.Admin;

public record ErrorQueueSummary(string Queue, string Consumer, long Length);

public record ErrorQueueMessage(
    string MessageId,
    DateTimeOffset EnqueuedTime,
    string ExceptionType,
    string ExceptionMessage,
    string? StackTrace,
    string? ConsumerType,
    string? OriginalMessageJson);

public class AdminErrorQueueService(ServiceBusAdministrationClient adminClient, ServiceBusClient client)
{
    private const string ErrorQueueSuffix = "_error";

    public static bool IsErrorQueue(string queue) => queue.EndsWith(ErrorQueueSuffix, StringComparison.Ordinal);

    public async Task<IReadOnlyList<ErrorQueueSummary>> ListQueuesAsync(CancellationToken ct = default)
    {
        var result = new List<ErrorQueueSummary>();
        await foreach (var queue in adminClient.GetQueuesAsync(ct))
        {
            if (!IsErrorQueue(queue.Name))
                continue;

            var runtime = await adminClient.GetQueueRuntimePropertiesAsync(queue.Name, ct);
            var length = runtime.Value.ActiveMessageCount;
            // AlmostServiceBus.TestHost 0.6.0's management API never serializes a queue's
            // message-count field at all (confirmed by direct testing, same gap already found for
            // subscriptions in the discarded topic-based design) — this reads 0 unconditionally
            // against that emulator. Falling back to a peek-based count only when the runtime
            // count reads 0 keeps real Azure's accurate count authoritative (a truly empty queue
            // just peeks empty too, so this is a no-op there) while making this queryable in tests.
            if (length == 0)
                length = await PeekCountAsync(queue.Name, ct);

            var consumer = queue.Name[..^ErrorQueueSuffix.Length];
            result.Add(new ErrorQueueSummary(queue.Name, consumer, length));
        }
        return result;
    }

    private async Task<long> PeekCountAsync(string queue, CancellationToken ct)
    {
        await using var receiver = client.CreateReceiver(queue);
        var peeked = await receiver.PeekMessagesAsync(50, cancellationToken: ct);
        return peeked.Count;
    }

    public async Task<IReadOnlyList<ErrorQueueMessage>> PeekMessagesAsync(
        string queue, int maxMessages = 50, CancellationToken ct = default)
    {
        await using var receiver = client.CreateReceiver(queue);
        var peeked = await receiver.PeekMessagesAsync(maxMessages, cancellationToken: ct);
        return peeked.Select(ToErrorQueueMessage).ToList();
    }

    private static ErrorQueueMessage ToErrorQueueMessage(ServiceBusReceivedMessage message)
    {
        var props = message.ApplicationProperties;
        var original = JsonNode.Parse(message.Body.ToString())?["message"];

        return new ErrorQueueMessage(
            message.MessageId,
            message.EnqueuedTime,
            GetProperty(props, "MT-Fault-ExceptionType") ?? "Unknown",
            GetProperty(props, "MT-Fault-Message") ?? "",
            GetProperty(props, "MT-Fault-StackTrace"),
            GetProperty(props, "MT-Fault-ConsumerType"),
            original?.ToJsonString());
    }

    private static string? GetProperty(IReadOnlyDictionary<string, object> props, string key) =>
        props.TryGetValue(key, out var value) ? value?.ToString() : null;
}
```

- [ ] **Step 5: Restore the fixture's `AdminErrorQueueService` registration**

Edit `src/FplBot.Tests/E2E/Admin/AdminErrorQueueFixture.cs`: uncomment `public AdminErrorQueueService Service => ...`, add back `services.AddSingleton<AdminErrorQueueService>();` in `InitializeAsync`, and add `using FplBot.WebApi.Admin;` to the top of the file.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental` then `./bin/Debug/net11.0/FplBot.Tests --filter-query "/*/*/AdminErrorQueueServiceListTests/*" "/*/*/AdminErrorQueueFixtureTests/*"`
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
- Produces: `AdminErrorQueueService.RetryMessageAsync(string queue, string messageId, CancellationToken)` and `.DiscardMessageAsync(string queue, string messageId, CancellationToken)`, both returning `Task<bool>` (`true` if a matching message was found and acted on) — used by Task 5. The shared scan helper (`ScanAndActAsync`) bounds itself by cycle detection (tracking already-abandoned message ids, stopping on the first repeat) — never by `ActiveMessageCount`. Retry resends the message's exact original body (no envelope reconstruction needed — unlike the discarded topic-based design, the `_error` queue message body is already a complete, valid MassTransit envelope) to the target queue, which is simply `queue` with the `_error` suffix stripped.

- [ ] **Step 1: Write the failing tests**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServiceRetryDiscardTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental`
Expected: FAIL (compile error) — `RetryMessageAsync`/`DiscardMessageAsync` don't exist yet.

- [ ] **Step 3: Implement retry and discard**

Add to `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs` (inside the `AdminErrorQueueService` class, after `PeekMessagesAsync`/`ToErrorQueueMessage`/`GetProperty`):

```csharp
    public Task<bool> RetryMessageAsync(string queue, string messageId, CancellationToken ct = default)
    {
        var targetQueue = queue[..^ErrorQueueSuffix.Length];
        return ScanAndActAsync(queue, messageId, async (receiver, message) =>
        {
            // The error-queue message body is already a complete, valid MassTransit envelope for
            // its original message type — no reconstruction needed, just resend it. A fresh
            // transport MessageId avoids any confusion with the completed original.
            await using var sender = client.CreateSender(targetQueue);
            var retryMessage = new ServiceBusMessage(message.Body) { ContentType = message.ContentType };
            await sender.SendMessageAsync(retryMessage, ct);

            await receiver.CompleteMessageAsync(message, ct);
        }, ct);
    }

    public Task<bool> DiscardMessageAsync(string queue, string messageId, CancellationToken ct = default) =>
        ScanAndActAsync(queue, messageId, (receiver, message) => receiver.CompleteMessageAsync(message, ct), ct);

    // There is no fetch-by-message-id in Azure Service Bus, so this scans the queue (PeekLock),
    // abandoning every non-matching message immediately so it returns to the queue. Bounded by
    // cycle detection, NOT by ActiveMessageCount (confirmed unreliable against this repo's test
    // emulator — see the class-level notes): abandoning a non-matching message makes it instantly
    // redeliverable, so once a message id repeats, the scan has cycled through everything in the
    // queue without finding the target — stop there.
    private async Task<bool> ScanAndActAsync(
        string queue, string messageId,
        Func<ServiceBusReceiver, ServiceBusReceivedMessage, Task> onMatch, CancellationToken ct)
    {
        await using var receiver = client.CreateReceiver(queue);
        var seen = new HashSet<string>();

        while (true)
        {
            var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), ct);
            if (message is null)
                break;

            if (message.MessageId == messageId)
            {
                await onMatch(receiver, message);
                return true;
            }

            if (!seen.Add(message.MessageId))
            {
                await receiver.AbandonMessageAsync(message, cancellationToken: ct);
                break;
            }

            await receiver.AbandonMessageAsync(message, cancellationToken: ct);
        }

        return false;
    }
```

No new `using` directives are needed — `Azure.Messaging.ServiceBus` and `System.Text.Json.Nodes` are already imported at the top of the file from Task 2.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental` then `./bin/Debug/net11.0/FplBot.Tests --filter-query "/*/*/AdminErrorQueueServiceRetryDiscardTests/*"`
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
- Produces: `AdminErrorQueueService.PurgeQueueAsync(string queue, CancellationToken)` returning `Task<int>` (count purged) — used by Task 5. Loops until a receive call returns zero messages — self-terminating (every message is completed, never abandoned back), so it needs no `ActiveMessageCount` bound.

- [ ] **Step 1: Write the failing test**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorQueueServicePurgeTests.cs`:

```csharp
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
            await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));

        foreach (var key in keys)
            Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var purged = await fixture.Service.PurgeQueueAsync(Queue);

        // >= rather than == : purge legitimately clears every message currently in the shared
        // queue, including any stray leftovers from an earlier test's failed cleanup — this test
        // only needs to know its own 3 keys are gone, not that purged is exactly 3.
        Assert.True(purged >= keys.Count, $"Expected at least {keys.Count} messages purged, got {purged}.");
        foreach (var key in keys)
        {
            var messages = await fixture.Service.PeekMessagesAsync(Queue);
            Assert.DoesNotContain(messages, m => m.OriginalMessageJson != null && m.OriginalMessageJson.Contains(key));
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental`
Expected: FAIL (compile error) — `PurgeQueueAsync` doesn't exist yet.

- [ ] **Step 3: Implement purge**

Add to `src/FplBot/Services/WebApi/Admin/AdminErrorQueueService.cs` (inside the class, after `ScanAndActAsync`):

```csharp
    // Not bounded by ActiveMessageCount (confirmed unreliable against this repo's test emulator).
    // Every message here is completed, never abandoned back, so looping until a receive call
    // returns nothing is already self-terminating and needs no external bound.
    public async Task<int> PurgeQueueAsync(string queue, CancellationToken ct = default)
    {
        await using var receiver = client.CreateReceiver(queue);
        var purged = 0;

        while (true)
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

Run: `cd src/FplBot.Tests && dotnet build --no-incremental` then `./bin/Debug/net11.0/FplBot.Tests --filter-query "/*/*/AdminErrorQueueServicePurgeTests/*"`
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
- Produces: `GET /api/admin/errors/queues`, `GET /api/admin/errors/queues/{queue}/messages`, `POST /api/admin/errors/queues/{queue}/messages/{messageId}/retry`, `POST /api/admin/errors/queues/{queue}/messages/{messageId}/discard`, `POST /api/admin/errors/queues/{queue}/purge` — consumed by the frontend in Tasks 6–8. `queue` is a normal path segment (queue names never contain `/`, unlike the discarded topic-based design's fault-topic names). Every handler that takes a `queue` calls `AdminErrorQueueService.IsErrorQueue(queue)` first and returns `TypedResults.BadRequest(...)` if false — this destructive surface must never reach a live, non-error queue.

- [ ] **Step 1: Write the failing tests**

Create `src/FplBot.Tests/E2E/Admin/AdminErrorEndpointsTests.cs`:

```csharp
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FplBot.Tests.E2E.Admin;

// Same rule as every earlier task's tests: identify this test's own message by its `key`, never
// by raw Length/ActiveMessageCount, since the error queue is shared across the whole collection.
[Collection("AdminErrorQueue")]
public class AdminErrorEndpointsTests(AdminErrorQueueFixture fixture)
{
    private const string Queue = AdminErrorQueueFixtureTests.ErrorQueueName;

    [Fact]
    public async Task GetQueues_ReturnsOkWithTheErrorQueue()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await AdminErrorEndpoints.GetQueues(fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<IReadOnlyList<ErrorQueueSummary>>>(result);
        Assert.Contains(ok.Value!, q => q.Queue == Queue);

        await fixture.DrainMatchingAsync(Queue, key);
    }

    [Fact]
    public async Task RetryMessage_UnknownId_ReturnsNotFound()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await AdminErrorEndpoints.RetryMessage(Queue, Guid.NewGuid().ToString(), fixture.Service, CancellationToken.None);

        Assert.IsType<NotFound>(result);

        await fixture.DrainMatchingAsync(Queue, key);
    }

    [Fact]
    public async Task RetryMessage_NonErrorQueue_ReturnsBadRequest()
    {
        var result = await AdminErrorEndpoints.RetryMessage("AlwaysFaultsHandler", "any-id", fixture.Service, CancellationToken.None);

        Assert.IsType<BadRequest<object>>(result);
    }

    [Fact]
    public async Task PurgeQueue_ReturnsPurgedCount()
    {
        var key = Guid.NewGuid().ToString();
        await fixture.Publisher.Publish(new PoisonTestMessage(key, AlwaysFault: true));
        Assert.True(await AdminErrorQueueFixtureTests.WaitForMessageAsync(fixture, Queue, key));

        var result = await AdminErrorEndpoints.PurgeQueue(Queue, fixture.Service, CancellationToken.None);

        var ok = Assert.IsType<Ok<PurgeResult>>(result);
        Assert.True(ok.Value!.Purged >= 1);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental`
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
        group.MapGet("/errors/queues/{queue}/messages", GetMessages);
        group.MapPost("/errors/queues/{queue}/messages/{messageId}/retry", RetryMessage);
        group.MapPost("/errors/queues/{queue}/messages/{messageId}/discard", DiscardMessage);
        group.MapPost("/errors/queues/{queue}/purge", PurgeQueue);
    }

    internal static async Task<IResult> GetQueues(AdminErrorQueueService service, CancellationToken ct)
        => TypedResults.Ok(await service.ListQueuesAsync(ct));

    internal static async Task<IResult> GetMessages(string queue, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        return TypedResults.Ok(await service.PeekMessagesAsync(queue, ct: ct));
    }

    internal static async Task<IResult> RetryMessage(string queue, string messageId, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        var retried = await service.RetryMessageAsync(queue, messageId, ct);
        return retried ? TypedResults.Ok(new { message = "Message retried" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> DiscardMessage(string queue, string messageId, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        var discarded = await service.DiscardMessageAsync(queue, messageId, ct);
        return discarded ? TypedResults.Ok(new { message = "Message discarded" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> PurgeQueue(string queue, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        var purged = await service.PurgeQueueAsync(queue, ct);
        return TypedResults.Ok(new PurgeResult(purged));
    }
}
```

- [ ] **Step 4: Register the service and map the endpoints**

Edit `src/FplBot/Services/WebApi/Infrastructure/WebApplicationBuilderExtensions.cs`. Add `using Azure.Messaging.ServiceBus;`, `using Azure.Messaging.ServiceBus.Administration;`, and `using FplBot.WebApi.Admin;` to the top, and add this block right after `services.AddIndexingServices(configuration, redisConn);`:

```csharp
        var asbConnectionString = configuration["ASB_CONNECTIONSTRING"]
            ?? throw new InvalidOperationException("Service bus connection string not configured. Set ASB_CONNECTIONSTRING.");
        services.AddSingleton(new ServiceBusAdministrationClient(asbConnectionString));
        services.AddSingleton(new ServiceBusClient(asbConnectionString));
        services.AddSingleton<AdminErrorQueueService>();
```

Edit `src/FplBot/Services/WebApi/Infrastructure/WebAppExtensions.cs`: add this line after `AdminHealthEndpoints.Map(admin, env);`:

```csharp
        AdminErrorEndpoints.Map(admin);
```

(`using FplBot.WebApi.Endpoints.Api.Admin;` is already present in this file for the other `Admin*Endpoints`.)

- [ ] **Step 5: Run the tests to verify they pass**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental` then `./bin/Debug/net11.0/FplBot.Tests --filter-query "/*/*/AdminErrorEndpointsTests/*"`
Expected: PASS (all 4 tests)

- [ ] **Step 6: Full build check**

Run: `dotnet build src/FplBot.slnx --no-incremental`
Expected: builds with no new warnings — confirms `WebApiService` still wires correctly end-to-end (the `ASB_CONNECTIONSTRING`-backed clients are constructed lazily, so this doesn't require a live Service Bus).

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
- Produces: TS interfaces `ErrorQueueSummary`, `ErrorQueueMessage` and functions `getErrorQueues()`, `getErrorQueueMessages(queue)`, `retryErrorMessage(queue, messageId)`, `discardErrorMessage(queue, messageId)`, `purgeErrorQueue(queue)` — used by Tasks 7 and 8. Every function takes a single `queue` string (URL-encoded where it appears in a path segment) — no `topic`/`subscription` pair, unlike the discarded topic-based design.

- [ ] **Step 1: Add the types**

Edit `src/FplBot/Services/WebApi/ClientApp/src/api/types.ts`, appending at the end of the file:

```typescript
// ---- Admin: error queues ----

export interface ErrorQueueSummary {
  queue: string;
  consumer: string;
  length: number;
}

export interface ErrorQueueMessage {
  messageId: string;
  enqueuedTime: string;
  exceptionType: string;
  exceptionMessage: string;
  stackTrace: string | null;
  consumerType: string | null;
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

export function getErrorQueueMessages(queue: string): Promise<ErrorQueueMessage[]> {
  return request(`/api/admin/errors/queues/${encodeURIComponent(queue)}/messages`);
}

export function retryErrorMessage(queue: string, messageId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/errors/queues/${encodeURIComponent(queue)}/messages/${encodeURIComponent(messageId)}/retry`);
}

export function discardErrorMessage(queue: string, messageId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/errors/queues/${encodeURIComponent(queue)}/messages/${encodeURIComponent(messageId)}/discard`);
}

export function purgeErrorQueue(queue: string): Promise<{ purged: number }> {
  return postJson(`/api/admin/errors/queues/${encodeURIComponent(queue)}/purge`);
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
- Produces: route `admin-errors-queues` at `/admin/errors`, and the router push target `{ name: 'admin-errors-queue-detail', params: { queue } }` that Task 8's detail view is reached from (a route **param**, not a query string — queue names never contain `/`, so this is a plain path segment, unlike the discarded topic-based design).

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
  if (!confirm(`Purge all ${queue.length} message(s) for "${queue.consumer}"? This cannot be undone.`)) return;
  purging.value = queue.queue;
  error.value = "";
  try {
    await purgeErrorQueue(queue.queue);
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
    <p class="lead">Faulted messages, one queue per consumer.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <p v-if="queues.length === 0">No error queues — nothing has faulted.</p>
        <table v-else class="admin-table">
          <thead>
            <tr>
              <th>Consumer</th>
              <th>Length</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="q in queues" :key="q.queue">
              <td>{{ q.consumer }}</td>
              <td>{{ q.length }}</td>
              <td class="row-actions">
                <router-link
                  class="btn small btn-secondary"
                  :to="{ name: 'admin-errors-queue-detail', params: { queue: q.queue } }"
                >
                  View
                </router-link>
                <button
                  class="btn small danger"
                  :disabled="purging === q.queue"
                  @click="purge(q)"
                >
                  {{ purging === q.queue ? "Purging..." : "Purge" }}
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

Edit `src/FplBot/Services/WebApi/ClientApp/src/router.ts`. Add this block to the `/admin` route's `children` array, right after the `search` block:

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
              path: ":queue",
              name: "admin-errors-queue-detail",
              component: () => import("./views/admin/ErrorQueueDetailView.vue"),
              props: true,
            },
          ],
        },
```

(`ErrorQueueDetailView.vue` is created in Task 8 — the dynamic `import()` means this compiles fine before that file exists at runtime, but this repo's `vue-tsc` DOES resolve dynamic import paths at typecheck time — a prior attempt at this plan found the typecheck step below fails without the file existing. Create a minimal placeholder now so this task's own typecheck passes, and let Task 8 replace its content entirely:

```vue
<template>
  <div>
    <h1>Error queue detail</h1>
    <p>Detail view (Task 8)</p>
  </div>
</template>
```

at `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue`. Remember to `git add` it along with everything else in this task's commit — don't leave it untracked.)

- [ ] **Step 4: Add the nav link**

Edit `src/FplBot/Services/WebApi/ClientApp/src/layouts/AdminLayout.vue`, update the `navLinks` array:

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
git add src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorsSection.vue src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueuesView.vue src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue src/FplBot/Services/WebApi/ClientApp/src/router.ts src/FplBot/Services/WebApi/ClientApp/src/layouts/AdminLayout.vue
git commit -m "Add Errors tab list view, purge action, and navigation"
```

---

## Task 8: Frontend — error queue detail view (messages, body, retry/discard)

**Files:**
- Modify (replacing the Task 7 placeholder): `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue`

**Interfaces:**
- Consumes: `getErrorQueueMessages()`, `retryErrorMessage()`, `discardErrorMessage()` (from Task 6), route prop `queue` (a route **param**, wired via `props: true` in Task 7 — not a query string).

- [ ] **Step 1: Replace the placeholder with the real detail view**

Overwrite `src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue`:

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getErrorQueueMessages, retryErrorMessage, discardErrorMessage } from "../../api/api";
import type { ErrorQueueMessage } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const props = defineProps<{ queue: string }>();

const messages = ref<ErrorQueueMessage[]>([]);
const loading = ref(true);
const error = ref("");
const acting = ref<string | null>(null);
const expanded = ref<Set<string>>(new Set());

async function load() {
  loading.value = true;
  error.value = "";
  try {
    messages.value = await getErrorQueueMessages(props.queue);
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
    await retryErrorMessage(props.queue, m.messageId);
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
    await discardErrorMessage(props.queue, m.messageId);
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
    <h1>{{ queue }}</h1>
    <p class="lead" v-if="!loading">{{ messages.length }} message(s) in this queue.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <p v-if="messages.length === 0">No messages.</p>
        <div v-for="m in messages" :key="m.messageId" class="message">
          <div class="message-header">
            <div>
              <div><b>{{ m.messageId }}</b></div>
              <div class="meta">{{ new Date(m.enqueuedTime).toLocaleString() }}</div>
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

          <p class="exception">
            <b>{{ m.exceptionType }}</b>: {{ m.exceptionMessage }}
            <span v-if="m.consumerType" class="consumer"> (in {{ m.consumerType }})</span>
          </p>

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

.exception {
  font-size: 0.9rem;
  color: #b91c1c;
  margin: 0.5rem 0;
}

.consumer {
  color: #6b7280;
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
Expected: builds with no new warnings

- [ ] **Step 4: Full test run**

Run: `cd src/FplBot.Tests && dotnet build --no-incremental` then `./bin/Debug/net11.0/FplBot.Tests --filter-query "/*/*/AdminErrorQueue*/*" "/*/*/AdminErrorEndpointsTests/*"`
Expected: all tests pass. (A full unfiltered run may show unrelated pre-existing Elasticsearch/Testcontainers flakiness in `SearchEndpointsTests`/`AdminSearchEndpointsTests` under constrained local Docker memory — that's environmental, not something this feature's tests should be judged against; the filtered run above is the one that matters here.)

- [ ] **Step 5: Manual smoke check (dev server)**

With the user's own `devenv.sh`/AppHost running (never start it yourself), open the admin UI, sign in, click the new "Errors" tab, and confirm it loads. This is a visual/UX check the automated tests above don't cover — flag to the user if you can't run it yourself.

- [ ] **Step 6: Commit**

```bash
git add src/FplBot/Services/WebApi/ClientApp/src/views/admin/ErrorQueueDetailView.vue
git commit -m "Add error queue detail view with message body inspection, retry, and discard"
```
