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
}
