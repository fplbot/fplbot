#!/usr/bin/env dotnet
#:package Azure.Messaging.ServiceBus@7.20.2

// Finds faulted messages on the local dev Service Bus emulator and, after
// confirmation, re-delivers each one to its original consumer queue so it
// gets consumed again.
//
// Useful when a consumer faulted for a reason that's since been fixed (e.g. a
// missing dev-mode guard around an external call) and you don't want to redo
// the whole flow (e.g. a real Slack OAuth install) just to get a fresh event.
// Works for any faulted message type — nothing here is specific to one
// consumer or message.
//
// Usage: ./RetryFaulted.cs   (requires chmod +x; run against local devenv only)
// For every faulted message found across every fault topic, prompts:
//   y = retry (re-deliver to its original consumer queue)
//   n = skip for now (leave it in the fault subscription for next time)
//   d = discard permanently (remove it from the fault subscription for good)

using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Messaging.ServiceBus;

const string connectionString = "Endpoint=sb://localhost:5672;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=emulator;UseDevelopmentEmulator=true";
const string dashboardBaseUrl = "http://localhost:15672";
const string @namespace = "default";

var prettyOptions = new JsonSerializerOptions { WriteIndented = true };

using var http = new HttpClient { BaseAddress = new Uri(dashboardBaseUrl) };
var entitiesJson = await http.GetStringAsync($"/api/dashboard/namespaces/{@namespace}/entities");
var entities = JsonNode.Parse(entitiesJson)
    ?? throw new InvalidOperationException("Could not reach the Service Bus emulator dashboard API.");

var faultTopics = entities["topics"]!.AsArray()
    .Where(t => t!["name"]!.GetValue<string>().StartsWith("MassTransit/Fault--"))
    .ToList();

// The peek endpoint's "state" field is unreliable for ForwardTo-configured subscriptions
// (a message can be reported "Active" there even after it's already been forwarded away
// and is no longer really receivable) — same root cause as the dashboard UI's count/click
// mismatch bug we found earlier. So `messageCount` from the entities API (which agrees with
// what a real receive actually finds) is the source of truth for *how many*; peeking is only
// used to look up a handler name to label that count with.
var handlerCounts = new Dictionary<string, int>();
foreach (var summaryTopic in faultTopics)
{
    var summaryTopicName = summaryTopic!["name"]!.GetValue<string>();
    foreach (var summarySub in summaryTopic["subscriptions"]!.AsArray())
    {
        var summarySubName = summarySub!["name"]!.GetValue<string>();
        var summaryCount = summarySub["messageCount"]!.GetValue<int>();
        if (summaryCount == 0)
            continue;

        var summaryHandler = summarySubName;
        var summaryMessagesJson = await http.GetStringAsync($"/api/dashboard/namespaces/{@namespace}/topics/{summaryTopicName}/messages");
        foreach (var m in JsonNode.Parse(summaryMessagesJson)!.AsArray())
        {
            if (m!["state"]!.GetValue<string>() != "Active")
                continue;
            using var summaryBody = JsonDocument.Parse(m["bodyText"]!.GetValue<string>());
            summaryHandler = summaryBody.RootElement.GetProperty("sourceAddress").GetString()!.Split('/').Last();
            break;
        }

        handlerCounts[summaryHandler] = handlerCounts.GetValueOrDefault(summaryHandler) + summaryCount;
    }
}

if (handlerCounts.Count == 0)
{
    Console.WriteLine("No faulted messages.");
    return;
}

Console.WriteLine("Faulted handlers:");
foreach (var (handler, count) in handlerCounts)
    Console.WriteLine($"  {handler}: {count}");
Console.WriteLine();

async Task<bool> TrySettleAsync(Func<Task> settle)
{
    try
    {
        await settle();
        return true;
    }
    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageLockLost)
    {
        // The peek-lock (~30s) can easily expire while a human reads/types an answer.
        // The server already makes the message available again on its own once the lock
        // expires, so there's nothing left for us to do here besides not crash.
        Console.WriteLine("(Lock expired before this could be settled — it's already back in the queue on its own.)");
        return false;
    }
}

await using var client = new ServiceBusClient(connectionString);
var handledAny = false;
var stopRequested = false;

foreach (var topic in faultTopics)
{
    if (stopRequested) break;
    var topicName = topic!["name"]!.GetValue<string>();
    foreach (var sub in topic["subscriptions"]!.AsArray())
    {
        if (stopRequested) break;
        var subName = sub!["name"]!.GetValue<string>();
        var receiver = client.CreateReceiver(topicName, subName);
        var skippedMessageIds = new HashSet<string>();

        while (!stopRequested)
        {
            var faultMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
            if (faultMessage is null)
                break;

            // Abandoning a skipped message makes it instantly redeliverable in this emulator,
            // so without this check we'd just be shown the same skipped message forever.
            if (skippedMessageIds.Contains(faultMessage.MessageId))
            {
                await TrySettleAsync(() => receiver.AbandonMessageAsync(faultMessage));
                break;
            }

            using var faultDoc = JsonDocument.Parse(faultMessage.Body.ToString());
            var fault = faultDoc.RootElement.GetProperty("message");
            var original = fault.GetProperty("message");
            var messageType = fault.GetProperty("faultMessageTypes")[0].GetString()!;
            var sourceAddress = faultDoc.RootElement.GetProperty("sourceAddress").GetString()!;
            var targetQueue = sourceAddress.Split('/').Last();

            Console.WriteLine();
            Console.WriteLine("──────────────────────────────────────");
            Console.WriteLine($"topic:        {topicName}");
            Console.WriteLine($"subscription: {subName}");
            Console.WriteLine($"messageId:    {faultMessage.MessageId}");
            Console.WriteLine($"will retry to: {targetQueue}");
            foreach (var ex in fault.GetProperty("exceptions").EnumerateArray())
                Console.WriteLine($"{ex.GetProperty("exceptionType").GetString()}: {ex.GetProperty("message").GetString()}");
            Console.WriteLine($"faultMessageType: {messageType}");
            Console.WriteLine(JsonNode.Parse(original.GetRawText())!.ToJsonString(prettyOptions));
            Console.Write("Retry this message? [y=retry, n=skip for now, d=discard permanently] ");

            var answer = Console.ReadLine();
            if (answer is null)
            {
                // Stdin closed (EOF) — abandon this message (leave it as-is for next time) and stop asking.
                await TrySettleAsync(() => receiver.AbandonMessageAsync(faultMessage));
                Console.WriteLine();
                Console.WriteLine("No more input — stopping.");
                stopRequested = true;
                break;
            }

            var trimmedAnswer = answer.Trim();

            if (string.Equals(trimmedAnswer, "d", StringComparison.OrdinalIgnoreCase))
            {
                if (await TrySettleAsync(() => receiver.CompleteMessageAsync(faultMessage)))
                    Console.WriteLine("Discarded.");
                continue;
            }

            if (!string.Equals(trimmedAnswer, "y", StringComparison.OrdinalIgnoreCase))
            {
                skippedMessageIds.Add(faultMessage.MessageId);
                await TrySettleAsync(() => receiver.AbandonMessageAsync(faultMessage));
                Console.WriteLine("Skipped.");
                continue;
            }

            if (!await TrySettleAsync(() => receiver.CompleteMessageAsync(faultMessage)))
            {
                // Couldn't mark the original fault as handled — don't send a duplicate retry.
                continue;
            }

            // urn:message:FplBot.Messaging.Contracts.Events.v1:AppInstalled -> FplBot.Messaging.Contracts.Events.v1/AppInstalled
            var typePath = messageType.Replace("urn:message:", "");
            var lastColon = typePath.LastIndexOf(':');
            typePath = typePath[..lastColon] + "/" + typePath[(lastColon + 1)..];

            var envelope = new JsonObject
            {
                ["messageId"] = Guid.NewGuid().ToString(),
                ["requestId"] = null,
                ["correlationId"] = null,
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["initiatorId"] = null,
                ["sourceAddress"] = "sb://localhost:5672/RetryFaulted",
                ["destinationAddress"] = $"sb://localhost/{typePath}?type=topic",
                ["responseAddress"] = null,
                ["faultAddress"] = null,
                ["messageType"] = new JsonArray(messageType),
                ["message"] = JsonNode.Parse(original.GetRawText()),
                ["expirationTime"] = null,
                ["sentTime"] = DateTimeOffset.UtcNow,
                ["headers"] = new JsonObject(),
            };

            var sender = client.CreateSender(targetQueue);
            var msg = new ServiceBusMessage(envelope.ToJsonString())
            {
                ContentType = "application/vnd.masstransit+json",
            };
            await sender.SendMessageAsync(msg);
            Console.WriteLine($"Sent fresh '{typePath}' message to '{targetQueue}'.");
            handledAny = true;
        }
    }
}

if (!handledAny)
    Console.WriteLine("No faulted messages were retried.");
