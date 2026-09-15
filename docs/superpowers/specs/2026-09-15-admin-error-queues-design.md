# Admin Error Queues — Design

## Problem

Faulted MassTransit messages are currently discarded globally
(`cfg.DiscardFaultedMessages()` in `Hosting/FplBotApplication.cs`).
There is no visibility into consumer failures and no way to retry a
failed message short of manually replaying the original trigger. We
want an "Errors" tab in the admin UI that lists error queues, their
lengths, a details view per queue, and a retry action per message.

## Scope

- Stop discarding faulted messages; let MassTransit route them to its
  default per-consumer `{ConsumerName}_error` queues on Azure Service
  Bus (queues are already named `nameof(Consumer)`, one per consumer
  class, per the existing MassTransit queue-naming convention).
- Add a read/manage layer over those `_error` queues, exposed as new
  admin API endpoints and a new admin UI tab.
- Out of scope: alerting/paging on error queue growth, auto-retry
  policies, retention/expiry changes beyond the existing 2-hour
  `DefaultMessageTimeToLive`.

## Architecture

Two independent changes:

1. **Stop discarding faults.** Remove the global
   `cfg.DiscardFaultedMessages()` call
   (`Hosting/FplBotApplication.cs:82-88`). This restores MassTransit's
   default behavior: a faulted message is moved to `{queue}_error`.
2. **New admin surface** to read and act on those queues, backed
   directly by Azure Service Bus — MassTransit has no API for this,
   so the admin layer talks to ASB directly via the official SDK.

## Components

- **`Azure.Messaging.ServiceBus` SDK** — new package reference in
  `FplBot.csproj`, used only from the WebApi service.
- **`AdminErrorQueueService`** (new, WebApi) wraps:
  - `ServiceBusAdministrationClient.GetQueuesAsync()` filtered to
    names ending in `_error`, using
    `RuntimeProperties.ActiveMessageCount` for queue length.
  - `ServiceBusReceiver.PeekMessagesAsync` per queue for the details
    view: message id, enqueued time, and MassTransit's fault
    application properties (exception type, message, stack trace).
    Peek is non-destructive (no lock), safe for a polling list view.
  - **Retry**: there is no fetch-by-message-id in ASB, so retry
    receives messages from the error queue (PeekLock), and for each:
    if its `MessageId` matches the target, forward its body +
    application properties to the *original* input queue via
    `ServiceBusSender`, then `CompleteMessageAsync` on the error-queue
    copy only after the forward succeeds; if it doesn't match,
    `AbandonMessageAsync` immediately so it returns to the queue. Cap
    the scan (e.g. stop after N messages or one full queue pass) so a
    stale/missing id can't loop forever.
- **New admin endpoints**, `Services/WebApi/Endpoints/Api/Admin/AdminErrorEndpoints.cs`,
  registered in `WebAppExtensions.cs` next to the other `Admin*Endpoints`
  groups (same `RequireAuthorization("IsAdmin")` gate):
  - `GET /admin/errors/queues` → `[{ name, originalQueue, length }]`
  - `GET /admin/errors/queues/{queue}/messages` → peeked messages with
    fault details
  - `POST /admin/errors/queues/{queue}/messages/{messageId}/retry`
- **New admin UI tab**, following the existing Slack/Discord/Search
  admin section pattern (Vue 3 SPA, `src/FplBot/Services/WebApi/ClientApp`):
  - `ErrorsSection.vue` + `ErrorQueuesView.vue` (list with lengths) +
    `ErrorQueueDetailView.vue` (messages in one queue, retry button)
  - Wired into `router.ts` (nested under the `/admin` route) and the
    `navLinks` array in `layouts/AdminLayout.vue`.

## Data flow

Consumer throws → MassTransit moves the message to
`{ConsumerName}_error` (instead of discarding) → admin UI calls
`GET /admin/errors/queues` for the list and lengths → selecting a
queue calls the messages endpoint (peek, non-destructive) → clicking
retry receives, forwards, and completes that one message → the next
list refresh shows the queue one message shorter (or unchanged, with
the message re-faulting back into the error queue, if the underlying
issue isn't fixed).

## Error handling / caveats

- Faulted messages now **persist** (subject to the existing 2-hour
  `DefaultMessageTimeToLive`) instead of vanishing. That's the
  intended behavior change, but it means error queues now need
  attention — nothing expires them from view besides that TTL.
- The Azure Service Bus connection string (`ASB_CONNECTIONSTRING`)
  already exists for MassTransit; the new SDK client reuses it — no
  new secret/config surface.
- The retry scan-and-abandon approach bumps delivery count on messages
  it passes over while searching for the target id. This is
  acceptable for an infrequent, manually-triggered admin action, but
  the implementation must bound the scan rather than loop unbounded.

## Testing

Integration test in `FplBot.Tests`, against the real local Azure
Service Bus emulator (same infra already used for other MassTransit
integration tests): force a consumer to fault, assert the message
lands in `{Consumer}_error`, call the list/detail endpoints, retry the
message, assert it disappears from the error queue and is reprocessed
by the original consumer.
