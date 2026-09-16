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
                    if (message.MessageId == messageId)
                    {
                        match = message;
                        break;
                    }
                    held.Add(message);
                }
            }

            if (match is not null)
                await onMatch(receiver, match);

            return match is not null;
        }
        finally
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
    }

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
}
