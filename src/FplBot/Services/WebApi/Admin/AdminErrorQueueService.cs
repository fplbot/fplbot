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
                var length = runtime.Value.ActiveMessageCount;
                // Some Service Bus-compatible test emulators (verified: AlmostServiceBus.TestHost
                // 0.6.0) never populate subscription message-count fields in their management API
                // response, so ActiveMessageCount always reads 0 there regardless of retries/delay,
                // even though the message is reliably peekable. Falling back to a peek count only
                // when the runtime count reads 0 keeps real Azure's accurate count authoritative
                // (a truly empty subscription just peeks empty too) while making this queryable
                // against such emulators.
                if (length == 0)
                    length = await PeekCountAsync(topic.Name, sub.SubscriptionName, ct);
                result.Add(new ErrorQueueSummary(topic.Name, sub.SubscriptionName, messageType, length));
            }
        }
        return result;
    }

    private async Task<long> PeekCountAsync(string topic, string subscription, CancellationToken ct)
    {
        await using var receiver = client.CreateReceiver(topic, subscription);
        var peeked = await receiver.PeekMessagesAsync(50, cancellationToken: ct);
        return peeked.Count;
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
    // Bounded by cycle detection, NOT by ActiveMessageCount (confirmed unreliable against this
    // repo's test emulator — see Task 2's notes; it also wouldn't be the right bound even against
    // real Azure, since it can change while the scan runs). Abandoning a non-matching message
    // makes it immediately redeliverable, so once a message id repeats, the scan has cycled through
    // every message currently in the subscription without finding the target — stop there.
    private async Task<bool> ScanAndActAsync(
        string topic, string subscription, string messageId,
        Func<ServiceBusReceiver, ServiceBusReceivedMessage, Task> onMatch, CancellationToken ct)
    {
        await using var receiver = client.CreateReceiver(topic, subscription);
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
}
