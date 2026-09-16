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
