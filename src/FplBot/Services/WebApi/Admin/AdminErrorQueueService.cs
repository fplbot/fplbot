using System.Collections.Concurrent;
using System.Text.Json;
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

public class AdminErrorQueueService(ServiceBusAdministrationClient adminClient, ServiceBusClient client, ILogger<AdminErrorQueueService> logger)
{
    private const string ErrorQueueSuffix = "_error";

    // Every receive-based operation below locks messages it looks at, and this transport's lock
    // duration is minutes, not seconds. Two overlapping operations on the same queue therefore
    // don't just interleave — the second one sees an empty queue (everything is locked by the
    // first), concludes "not found", and reports a false failure. One gate per queue makes them
    // queue up instead. Receive-based operations on *different* queues never contend, so the gate
    // is per queue name rather than global.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> QueueGates = new();

    // An _error queue has no consumer, so a message's DeliveryCount only ever counts how many
    // times an admin scan here has picked it up — including scans looking for a *different*
    // message in the same queue. Against the transport default (10, or MassTransit's 5) that
    // silently dead-letters innocent bystanders after a handful of admin actions, and a
    // dead-lettered message drops out of PeekMessagesAsync, so it vanishes from the UI entirely.
    // Raise the budget once per queue per process so routine admin use can't destroy the very
    // messages this tool exists to recover.
    private const int ScanDeliveryBudget = 50;
    private static readonly ConcurrentDictionary<string, bool> BudgetEnsured = new();

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
        return [.. peeked.Select(ToErrorQueueMessage)];
    }

    private static ErrorQueueMessage ToErrorQueueMessage(ServiceBusReceivedMessage message)
    {
        var props = message.ApplicationProperties;
        var bodyText = message.Body.ToString();
        string? originalMessageJson;
        try
        {
            originalMessageJson = JsonNode.Parse(bodyText)?["message"]?.ToJsonString();
        }
        catch (JsonException)
        {
            // Not a valid MassTransit JSON envelope — fall back to the raw body text so the
            // frontend's existing non-JSON fallback (prettyBody() in ErrorQueueDetailView.vue)
            // has something to render, instead of crashing the whole queue's peek.
            originalMessageJson = bodyText;
        }

        return new ErrorQueueMessage(
            message.MessageId,
            message.EnqueuedTime,
            GetProperty(props, "MT-Fault-ExceptionType") ?? "Unknown",
            GetProperty(props, "MT-Fault-Message") ?? "",
            GetProperty(props, "MT-Fault-StackTrace"),
            GetProperty(props, "MT-Fault-ConsumerType"),
            originalMessageJson);
    }

    private static string? GetProperty(IReadOnlyDictionary<string, object> props, string key) =>
        props.TryGetValue(key, out var value) ? value?.ToString() : null;

    public Task<bool> RetryMessageAsync(string queue, string messageId, CancellationToken ct = default)
    {
        var targetQueue = queue[..^ErrorQueueSuffix.Length];
        return ScanAndActAsync(queue, messageId, async (receiver, message) =>
        {
            await using var sender = client.CreateSender(targetQueue);
            await sender.SendMessageAsync(ToRetryMessage(message), ct);
            await receiver.CompleteMessageAsync(message, ct);
        }, ct);
    }

    public Task<bool> DiscardMessageAsync(string queue, string messageId, CancellationToken ct = default) =>
        ScanAndActAsync(queue, messageId, (receiver, message) => receiver.CompleteMessageAsync(message, ct), ct);

    // The error-queue message body is already a complete, valid MassTransit envelope for its
    // original message type — no reconstruction needed, just resend it. The MT-Fault-* headers are
    // deliberately not carried over: a retry is a fresh delivery attempt, not a fault. The
    // MessageId is, so a message that faults again comes back under the id the operator just acted
    // on — otherwise every retry makes it reappear as a brand new broker-assigned id, which reads
    // as "retry did nothing" rather than "it ran and failed again".
    private static ServiceBusMessage ToRetryMessage(ServiceBusReceivedMessage message) =>
        new(message.Body) { ContentType = message.ContentType, MessageId = message.MessageId };

    // There is no fetch-by-message-id in Azure Service Bus, so this scans the queue (PeekLock).
    // Abandoning a message makes it immediately redeliverable, and this transport delivers in
    // order — so abandoning non-matching messages one at a time as we go would just keep handing
    // us the same head-of-queue message back before we ever reached a target sitting deeper in
    // the queue. Instead: receive whole batches, keep every held message's lock until we've either
    // found the match or exhausted the queue, and only then abandon the non-matching ones (once,
    // at the end). Because every message we've looked at stays locked throughout, none of them
    // become redeliverable mid-scan, so message order can't fool this into stopping early.
    // Capped at maxScanned total messages as a safety net, consistent with this file's other
    // bounded scans (PurgeQueueAsync, PeekCountAsync) — not by ActiveMessageCount, which is
    // confirmed unreliable against this repo's test emulator (see class-level notes).
    private async Task<bool> ScanAndActAsync(
        string queue, string messageId,
        Func<ServiceBusReceiver, ServiceBusReceivedMessage, Task> onMatch, CancellationToken ct)
    {
        const int maxScanned = 1000;
        using var gate = await EnterQueueAsync(queue, ct);
        await using var receiver = client.CreateReceiver(queue);
        var held = new List<ServiceBusReceivedMessage>();
        ServiceBusReceivedMessage? match = null;

        try
        {
            while (match is null && held.Count < maxScanned)
            {
                var batch = await receiver.ReceiveMessagesAsync(maxMessages: 100, maxWaitTime: TimeSpan.FromSeconds(5), cancellationToken: ct);
                if (batch.Count == 0)
                    break;

                foreach (var message in batch)
                {
                    if (match is null && message.MessageId == messageId)
                    {
                        match = message;
                        continue;
                    }

                    // Everything received that isn't the match is held for release below — the
                    // rest of the match's own batch included. Dropping those on the floor instead
                    // would leave them locked for the full lock duration (minutes on this
                    // transport), and a locked message is invisible to the next scan, so the very
                    // next retry on one of them reports "not found" for a message that is there.
                    held.Add(message);
                }
            }

            if (match is not null)
                await onMatch(receiver, match);

            return match is not null;
        }
        finally
        {
            await ReleaseAsync(receiver, held, ct);
        }
    }

    // Not bounded by ActiveMessageCount (confirmed unreliable against this repo's test emulator).
    // Every message here is completed, never abandoned back, so looping until a receive call
    // returns nothing is already self-terminating and needs no external bound.
    public async Task<int> PurgeQueueAsync(string queue, CancellationToken ct = default)
    {
        using var gate = await EnterQueueAsync(queue, ct);
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

    // Resends every message in the queue in one drain. Unlike repeated single retries this never
    // scans for a specific id, so it can't miss a message sitting deeper in the queue and doesn't
    // spend a delivery attempt on every bystander it passes.
    //
    // Bounded to the messages already in the queue rather than "until the queue is empty": when
    // the consumer is still broken every resend faults straight back into this same queue, and an
    // unbounded drain would keep picking up its own output. The bound is the highest sequence
    // number present when the run starts — broker-assigned and monotonic, so unlike a timestamp
    // comparison it can't be thrown off by the app's clock sitting ahead of or behind the bus.
    // Anything above it is held rather than abandoned one by one, which keeps it invisible to this
    // receiver for the rest of the run (that is what lets the loop finish) and released at the end.
    public async Task<int> RetryAllMessagesAsync(string queue, CancellationToken ct = default)
    {
        const int maxRetried = 1000;
        var targetQueue = queue[..^ErrorQueueSuffix.Length];

        using var gate = await EnterQueueAsync(queue, ct);
        await using var receiver = client.CreateReceiver(queue);

        var present = await receiver.PeekMessagesAsync(maxRetried, cancellationToken: ct);
        if (present.Count == 0)
            return 0;
        var highestAtStart = present.Max(m => m.SequenceNumber);

        await using var sender = client.CreateSender(targetQueue);
        var held = new List<ServiceBusReceivedMessage>();
        var retried = 0;

        try
        {
            while (retried < maxRetried)
            {
                var batch = await receiver.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromSeconds(5), cancellationToken: ct);
                if (batch.Count == 0)
                    break;

                foreach (var message in batch)
                {
                    if (message.SequenceNumber > highestAtStart)
                    {
                        held.Add(message);
                        continue;
                    }

                    await sender.SendMessageAsync(ToRetryMessage(message), ct);
                    await receiver.CompleteMessageAsync(message, ct);
                    retried++;
                }
            }

            return retried;
        }
        finally
        {
            await ReleaseAsync(receiver, held, ct);
        }
    }

    private static async Task ReleaseAsync(ServiceBusReceiver receiver, List<ServiceBusReceivedMessage> held, CancellationToken ct)
    {
        foreach (var message in held)
        {
            try
            {
                await receiver.AbandonMessageAsync(message, cancellationToken: ct);
            }
            catch
            {
                // Best effort — a lock may have expired if the scan ran long; nothing more to do.
            }
        }
    }

    private async Task<IDisposable> EnterQueueAsync(string queue, CancellationToken ct)
    {
        await EnsureScanBudgetAsync(queue, ct);
        var gate = QueueGates.GetOrAdd(queue, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        return new QueueGate(gate);
    }

    private async Task EnsureScanBudgetAsync(string queue, CancellationToken ct)
    {
        if (BudgetEnsured.ContainsKey(queue))
            return;

        try
        {
            var properties = (await adminClient.GetQueueAsync(queue, ct)).Value;
            if (properties.MaxDeliveryCount < ScanDeliveryBudget)
            {
                properties.MaxDeliveryCount = ScanDeliveryBudget;
                await adminClient.UpdateQueueAsync(properties, ct);
            }
        }
        catch (Exception e)
        {
            // Never fail an admin action over this — without it the worst case is the behaviour
            // that already existed (bystanders dead-letter sooner), not a broken retry.
            logger.LogWarning(e, "Could not raise MaxDeliveryCount on {Queue}", queue);
        }
        finally
        {
            // Marked whether or not it worked: if the bus denies this (no Manage rights, say),
            // retrying it on every single admin action would put a failed round-trip and a log
            // line in front of every click, forever, for something that is an optimisation.
            BudgetEnsured[queue] = true;
        }
    }

    private sealed class QueueGate(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
