# Admin Error Queues — Design

## Problem

There is no admin visibility into consumer failures and no way to
retry a failed message short of manually replaying the original
trigger. We want an "Errors" tab in the admin UI that lists error
queues, their lengths, a details view per queue (including message
bodies), and actions to retry a message, discard a single message, or
purge an entire queue.

## Revision note

An earlier version of this spec was built around MassTransit's
`Fault<T>` **topic** mechanism (`MassTransit/Fault--<type>--`). That
version was fully implemented and passed review, but the final
whole-branch review flagged a fatal, previously-unverified assumption:
the fault topic's only subscription had `ForwardTo` set to a
subscription-less topic, and Azure Service Bus (a) auto-forwards
messages out of a source entity immediately when `ForwardTo` is set,
and (b) does not allow creating a receiver on an auto-forwarding
source entity at all. Every prior verification of that design was
against `AlmostServiceBus.TestHost`, a third-party emulator that
apparently doesn't enforce that restriction — so the topic-based
design likely would not have worked against real Azure Service Bus at
all (messages would auto-forward away before anything could read
them, and `CreateReceiver` itself might throw).

Per direction from the repo owner: abide by MassTransit's own default
behavior rather than routing around it. This revision replaces the
topic-based design with MassTransit's standard `{queue}_error` **queue**
mechanism — a plain queue, no `ForwardTo`, safe to receive/peek/purge
on both the local emulator and real Azure.

## Ground truth: how faults actually surface

Verified directly against an isolated MassTransit/ASB bus (in-process,
via `AlmostServiceBus.TestHost`) — plain queues carry none of the
`ForwardTo` risk the topic-based approach had, since queues aren't a
pub/sub construct:

- When a consumer throws and the receive endpoint does **not** call
  `cfg.DiscardFaultedMessages()`, MassTransit moves the faulted message
  to a queue named `{OriginalQueueName}_error` — one queue per
  *consumer*, matching this repo's existing queue-naming convention
  (`nameof(Consumer)`), so `AlwaysFaultsHandler` produces
  `AlwaysFaultsHandler_error`.
- `DiscardFaultedMessages()` is what suppresses this — it makes
  MassTransit discard the faulted message instead of moving it. This
  repo's production bus currently calls it
  (`Hosting/FplBotApplication.cs:86`). **This setting must be removed**
  for this feature to have anything to read — this is a real,
  intentional messaging-pipeline change (unlike the discarded topic
  design, which mistakenly concluded no pipeline change was needed).
- Exception/fault details are attached as **message application
  properties** on the moved message, not embedded in the JSON body:
  `MT-Reason` ("fault"), `MT-Fault-ExceptionType`, `MT-Fault-Message`,
  `MT-Fault-StackTrace`, `MT-Fault-Timestamp`, `MT-Fault-ConsumerType`,
  `MT-Fault-MessageType`, `MT-Fault-InputAddress` (the original
  queue's address — confirmed to end in the real consumer name, e.g.
  `.../AlwaysFaultsHandler`).
- The message **body** is the original MassTransit envelope, completely
  unmodified — `envelope.message` is the original payload directly (no
  nested `fault` wrapper the way the old `Fault<T>` topic design had).
  This means retry is simpler than the old design: resend the exact
  same body/properties to the target queue (no envelope reconstruction,
  no URN parsing) — just a fresh `messageId`/`conversationId`.
- The target queue for a retry is simply the current queue's name with
  the `_error` suffix stripped (`AlwaysFaultsHandler_error` →
  `AlwaysFaultsHandler`) — no need to parse `MT-Fault-InputAddress`,
  though it agrees and could be used as a cross-check.
- `ServiceBusAdministrationClient.GetQueueRuntimePropertiesAsync(...)
  .ActiveMessageCount` has the same gap already found for subscriptions
  in the discarded design: confirmed by direct testing that it reads 0
  unconditionally against `AlmostServiceBus.TestHost`, even when a
  message is genuinely present and peekable. Same fallback needed: peek
  for a count when the runtime property reads 0.

## Scope

- Remove `cfg.DiscardFaultedMessages()` from
  `Hosting/FplBotApplication.cs` so faulted messages accumulate in
  `{consumer}_error` queues instead of being discarded. This is the one
  intentional messaging-pipeline change in this feature.
- Read/manage layer over those `_error` queues: list them with
  lengths, view messages (fault details + body) in one, retry a
  message, discard a single message, purge an entire queue.
- New admin API endpoints + a new "Errors" admin UI tab.
- Out of scope: alerting on queue growth, auto-retry policies,
  retention/TTL changes, dead-letter-subqueue routing. Faulted
  messages are still purged after the same 2-hour
  `DefaultMessageTimeToLive` as every other queue on this bus — raised
  explicitly and accepted.

## Architecture

Two changes:

1. **Stop discarding faults.** Remove the global
   `cfg.DiscardFaultedMessages()` call so MassTransit's default
   behavior applies.
2. **New admin surface** to read and act on the resulting `_error`
   queues, backed directly by Azure Service Bus (MassTransit has no
   read API for this).

## Components

- **`Azure.Messaging.ServiceBus` SDK** — package reference in
  `FplBot.csproj`, used only from the WebApi service. Reuses the
  existing `ASB_CONNECTIONSTRING` config value — no new secret/config
  surface.
- **`AdminErrorQueueService`** (WebApi), wrapping a
  `ServiceBusAdministrationClient` + `ServiceBusClient`:
  - **List**: `GetQueuesAsync()` filtered to names ending in `_error`;
    for each, `GetQueueRuntimePropertiesAsync(name).ActiveMessageCount`
    for length, falling back to a peek-based count when that reads 0
    (same gap as before, now confirmed for queues too). Labeled by the
    queue name with the `_error` suffix stripped — this **is** the
    consumer name, no parsing/guessing needed.
  - **Peek** (`ServiceBusReceiver` via `client.CreateReceiver(queueName)`,
    `PeekMessagesAsync`, non-destructive — safe for a polling list
    view): returns messageId, enqueued time, the `MT-Fault-*`
    application properties (exception type, message, stack trace,
    consumer type, timestamp), and the original payload
    (`envelope.message`) for the UI to pretty-print, with a raw text
    fallback if it isn't valid JSON.
  - **Retry** (single message, by messageId): no fetch-by-id in ASB, so
    this receives messages from the queue (PeekLock) and for each: if
    `MessageId` matches, build a fresh `ServiceBusMessage` from the
    *same* body and content type (no JSON reconstruction needed — the
    body is already a valid envelope), regenerate `messageId` in that
    body's JSON, send it to the target queue (current queue name minus
    `_error`), then `CompleteMessageAsync` the original only after the
    send succeeds; non-matches are `AbandonMessageAsync`'d immediately.
    Bounded by tracking already-abandoned message ids and stopping the
    moment one repeats (a full cycle with no match) — never bounded by
    `ActiveMessageCount`.
  - **Discard** (single message, by messageId): same scan-for-match
    logic as retry, but on match just `CompleteMessageAsync` — no
    resend.
  - **Purge** (whole queue): loop `ReceiveMessagesAsync` (batched) +
    `CompleteMessageAsync` each, until a receive call returns zero
    messages — self-terminating, no `ActiveMessageCount` bound needed.
- **New admin endpoints**,
  `Services/WebApi/Endpoints/Api/Admin/AdminErrorEndpoints.cs`,
  registered in `WebAppExtensions.cs` next to the other
  `Admin*Endpoints` groups (same `RequireAuthorization("IsAdmin")`
  gate):
  - `GET /admin/errors/queues` → `[{ queue, consumer, length }]`
  - `GET /admin/errors/queues/{queue}/messages` → peeked messages with
    fault details + body
  - `POST /admin/errors/queues/{queue}/messages/{messageId}/retry`
  - `POST /admin/errors/queues/{queue}/messages/{messageId}/discard`
  - `POST /admin/errors/queues/{queue}/purge`

  Queue names never contain a `/`, so `queue` is a normal path segment
  here — unlike the discarded topic-based design, there's no encoding
  problem to work around.
- **Guard**: every endpoint must reject a `queue` that doesn't end in
  `_error` (`BadRequest`), so a typo'd or copy-pasted queue name can't
  reach retry/discard/purge against a live, non-error queue. This is a
  destructive admin surface; it should only ever reach `_error` queues.
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

Consumer throws → MassTransit moves the message to
`{Consumer}_error` (now that `DiscardFaultedMessages()` is removed) →
admin UI calls `GET /admin/errors/queues` for the list and lengths →
selecting a queue calls the messages endpoint (peek, non-destructive)
→ retry/discard act on one message by id; purge clears the whole
queue → the next list refresh reflects the new counts (a retried
message either succeeds, emptying the queue further, or faults again
and reappears).

## Error handling / caveats

- The scan-and-abandon approach (retry/discard by message id) bumps
  delivery count on messages it passes over. Acceptable for an
  infrequent, manually-triggered admin action; bounded by tracking
  already-abandoned message ids and stopping on the first repeat —
  never by `ActiveMessageCount` (confirmed unreliable against this
  repo's test emulator, for both queues and the earlier subscription
  design).
- Purge is destructive and irreversible — the UI must confirm before
  calling it (same pattern as the existing "Uninstall" button in
  `SlackWorkspacesView.vue`).
- Every retry/discard/purge call is guarded against acting on a queue
  that doesn't end in `_error` — this feature must never be usable to
  drain a live, non-error queue.
- Faulted messages are still purged by ASB itself after the same
  2-hour `DefaultMessageTimeToLive` as every other queue on this bus —
  accepted trade-off, no retention change in scope.
- Removing `DiscardFaultedMessages()` means faulted messages now
  persist (subject to that same 2-hour TTL) instead of vanishing
  immediately — this is the intended behavior change and was the
  entire point of the original (now-corrected) design; re-flagging it
  here since the pipeline change is new to this revision.

## Testing

`AppFixture` (used by every other E2E test in `FplBot.Tests`) wires
MassTransit with `UsingInMemory(...)`, which has no error-queue/ASB
concepts at all — unusable for this feature. Instead, use
`AlmostServiceBus.TestHost` (already in this repo's dependency
ecosystem — it backs the local dev emulator via
`AlmostServiceBus.Aspire.Hosting` in `FplBot.AppHost`), whose
`ServiceBusEmulatorFixture` runs an in-process, per-test-isolated ASB
emulator with no Docker and sub-second startup. A fixture wires a real
`UsingAzureServiceBus(...)` bus against that emulator's connection
string — **without** `DiscardFaultedMessages()`, matching the
corrected production config — plus a small always-faulting test-only
consumer to produce real faults on demand.

Test coverage: force a fault, assert it appears in the list/peek
endpoints (queue name, application properties, body); retry it and
assert the queue empties and the consumer reprocesses it; discard it
(gone, no reprocess); purge a queue with multiple faults in one call;
confirm the `_error`-suffix guard rejects a non-error queue name.

Caveat found while building the fixture: `ActiveMessageCount` can read
0 against `AlmostServiceBus.TestHost` even when a message is genuinely
present and peekable (confirmed for both queues and subscriptions —
its management API never serializes message-count fields at all, not
a timing lag). Tests that need to assert "the fault has landed" should
poll via peek/receive of the actual message, not by polling
`ActiveMessageCount` alone.

## Residual risk

This design is queue-based, which avoids the specific `ForwardTo`
problem that broke the topic-based version. It has not been verified
against a real Azure Service Bus namespace (only against
`AlmostServiceBus.TestHost`) — plain queues are a much better-supported
emulator feature with no pub/sub forwarding semantics to get wrong,
so the risk is materially lower, but this is still worth a real check
before or shortly after shipping.
