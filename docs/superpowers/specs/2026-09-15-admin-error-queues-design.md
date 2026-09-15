# Admin Error Queues — Design

## Problem

There is no admin visibility into consumer failures and no way to
retry a failed message short of manually replaying the original
trigger. We want an "Errors" tab in the admin UI that lists error
queues, their lengths, a details view per queue (including message
bodies), and actions to retry a message, discard a single message, or
purge an entire queue.

## Ground truth: how faults actually surface today

Verified against `dev/RetryFaulted.cs` and the local Service Bus
emulator dashboard (`http://localhost:15672`), which already reads
this live — this is **not** MassTransit's textbook default topology,
so it's worth stating precisely:

- When a consumer throws, MassTransit publishes a `Fault<T>` event to
  a topic named `MassTransit/Fault--<message-type>--` (one fault topic
  per *original message type*, not per consumer).
- Each consumer that consumes that message type has its own
  **subscription** on that fault topic, named after the consumer
  (e.g. `AppInstalledHandler`).
- `cfg.DiscardFaultedMessages()` (`Hosting/FplBotApplication.cs:86`)
  only controls whether the *original* transport message is forwarded
  to an error queue after a fault — it does **not** suppress the
  `Fault<T>` publish. So fault visibility already exists today,
  independent of that setting, and **no messaging-pipeline change is
  needed for this feature.**
- The fault message body is a JSON envelope with `sourceAddress`
  (last path segment = the original consumer's input queue — the
  retry target), `faultMessageTypes` (MassTransit type URN(s)),
  `exceptions[]` (`exceptionType`/`message` per attempt), and the
  original payload nested at `message.message`.
- Retrying means: complete the fault message off its subscription,
  reconstruct a fresh MassTransit envelope wrapping the original
  payload, and send it directly to the target queue — exactly what
  `dev/RetryFaulted.cs` already does, interactively, via the
  `Azure.Messaging.ServiceBus` SDK (no MassTransit hosting involved).
  This admin feature formalizes that same proven mechanism behind a UI
  instead of a terminal prompt loop.

## Scope

- Read/manage layer over the *existing* fault topics/subscriptions:
  list them with lengths, view messages (fault details + body) in one,
  retry a message, discard a single message, purge an entire queue
  (all messages in one subscription).
- New admin API endpoints + a new "Errors" admin UI tab.
- Out of scope: any change to MassTransit fault/retry pipeline
  configuration, alerting on queue growth, auto-retry policies,
  retention/TTL changes. Faulted messages are still purged after the
  same 2-hour `DefaultMessageTimeToLive` as every other MassTransit
  queue/topic in this bus — raised explicitly and accepted; no
  dead-letter-subqueue routing or TTL override is in scope.

## Architecture

One addition: a new admin-only surface, backed directly by Azure
Service Bus (MassTransit has no read API for this — the admin layer
talks to ASB directly via the official SDK, the same way
`dev/RetryFaulted.cs` does).

## Components

- **`Azure.Messaging.ServiceBus` SDK** — new package reference in
  `FplBot.csproj`, used only from the WebApi service. Reuses the
  existing `ASB_CONNECTIONSTRING` config value (already used by
  MassTransit) — no new secret/config surface.
- **`AdminErrorQueueService`** (new, WebApi), wrapping a
  `ServiceBusAdministrationClient` + `ServiceBusClient`:
  - **List**: `GetTopicsAsync()` filtered to names starting with
    `MassTransit/Fault--`; for each, `GetSubscriptionsAsync(topicName)`
    then `GetSubscriptionRuntimePropertiesAsync(topicName, subName)
    .ActiveMessageCount` for length. Returned to the UI as one row per
    (topic, subscription) pair, labeled by the subscription name (the
    consumer/handler name) since that's what an admin recognizes.
  - **Peek** (`ServiceBusReceiver` via
    `client.CreateReceiver(topicName, subscriptionName)`,
    `PeekMessagesAsync`, non-destructive/no lock — safe for a polling
    list view): returns messageId, enqueued time, `sourceAddress`,
    `faultMessageTypes`, `exceptions[]`, and the original payload
    (`message.message`) for the UI to pretty-print (JSON, collapsible),
    with a raw text fallback if it isn't valid JSON.
  - **Retry** (single message, by messageId): there's no fetch-by-id in
    ASB, so this receives messages from the subscription (PeekLock)
    and for each: if `MessageId` matches, parse the fault envelope,
    build a fresh MassTransit envelope around the original payload
    (same shape as `dev/RetryFaulted.cs` — `messageId` regenerated,
    `conversationId` regenerated, `sourceAddress` identifying this
    admin action, `destinationAddress` derived from the message type
    URN, `messageType`, `message`, `sentTime`), send it via
    `client.CreateSender(targetQueue)` where `targetQueue` is the last
    segment of the original `sourceAddress`, then
    `CompleteMessageAsync` the fault message only after the send
    succeeds; non-matches are `AbandonMessageAsync`'d immediately so
    they return to the subscription. Cap the scan (stop after one full
    subscription pass) so a stale/missing id can't loop forever.
  - **Discard** (single message, by messageId): same scan-for-match
    logic as retry, but on match just `CompleteMessageAsync` — no
    resend.
  - **Purge** (whole subscription): read the subscription's
    `ActiveMessageCount` up front as a bound, then loop
    `ReceiveMessagesAsync` (batched) + `CompleteMessageAsync` each
    until that many messages have been completed or a receive times
    out empty — avoids looping forever if new faults land mid-purge.
- **New admin endpoints**,
  `Services/WebApi/Endpoints/Api/Admin/AdminErrorEndpoints.cs`,
  registered in `WebAppExtensions.cs` next to the other
  `Admin*Endpoints` groups (same `RequireAuthorization("IsAdmin")`
  gate):
  - `GET /admin/errors/queues` → `[{ topic, subscription, length }]`
  - `GET /admin/errors/queue/messages?topic=&subscription=` → peeked
    messages with fault details + body
  - `POST /admin/errors/queue/messages/{messageId}/retry?topic=&subscription=`
  - `POST /admin/errors/queue/messages/{messageId}/discard?topic=&subscription=`
  - `POST /admin/errors/queue/purge?topic=&subscription=`

  (`topic` and `subscription` are both needed to identify a queue — a
  consumer handling more than one message type can have same-named
  subscriptions on different fault topics. `topic` is passed as a
  query parameter rather than a path segment because fault topic names
  contain a literal `/`, e.g. `MassTransit/Fault--...--`, which would
  break path-segment route matching.)
- **New admin UI tab**, following the existing Slack/Discord/Search
  admin section pattern (Vue 3 SPA,
  `src/FplBot/Services/WebApi/ClientApp`):
  - `ErrorsSection.vue` + `ErrorQueuesView.vue` (list: consumer name,
    length, a "Purge" button per row) + `ErrorQueueDetailView.vue`
    (messages in one queue: fault details, a pretty-printed/collapsible
    view of the message body, and "Retry"/"Discard" buttons per
    message).
  - Wired into `router.ts` (nested under the `/admin` route) and the
    `navLinks` array in `layouts/AdminLayout.vue`.

## Data flow

Consumer throws → MassTransit publishes `Fault<T>` to
`MassTransit/Fault--<type>--`, landing in the consumer's existing
subscription (already happens today, unaffected by this feature) →
admin UI calls `GET /admin/errors/queues` for the list and lengths →
selecting a queue calls the messages endpoint (peek, non-destructive)
→ retry/discard act on one message by id; purge clears the whole
subscription → the next list refresh reflects the new counts (a
retried message either succeeds, emptying the subscription further, or
faults again and reappears).

## Error handling / caveats

- The scan-and-abandon approach (retry/discard by message id) bumps
  delivery count on messages it passes over while searching for the
  target. Acceptable for an infrequent, manually-triggered admin
  action, but the implementation must bound the scan (one pass) rather
  than loop unbounded.
- Purge is destructive and irreversible — the UI must ask for
  confirmation before calling it (same pattern as the existing
  "Uninstall" button in `SlackWorkspacesView.vue`).
- Faulted messages are still purged by ASB itself after the same
  2-hour `DefaultMessageTimeToLive` as every other queue/topic on this
  bus — accepted trade-off, no retention change in scope.

## Testing

`AppFixture` (used by every other E2E test in `FplBot.Tests`) wires
MassTransit with `UsingInMemory(...)`, which has no fault topics/ASB
concepts at all — unusable for this feature. Instead, use
`AlmostServiceBus.TestHost` (already in this repo's dependency
ecosystem — it backs the local dev emulator via
`AlmostServiceBus.Aspire.Hosting` in `FplBot.AppHost`), whose
`ServiceBusEmulatorFixture` runs an in-process, per-test-isolated ASB
emulator with no Docker and sub-second startup. A new fixture wires a
real `UsingAzureServiceBus(...)` bus against that emulator's
connection string, plus a small always-faulting test-only consumer to
produce real faults on demand (rather than relying on an existing
production consumer's business logic to fail in a controlled way).

Test coverage: force a fault, assert it appears in the list/peek
endpoints including the body; retry it and assert the subscription
empties and the consumer reprocesses it; discard it (gone, no
reprocess); purge a queue with multiple faults in one call.
