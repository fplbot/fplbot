---
name: retry-faulted
description: >
  Re-deliver a faulted message from the local dev Service Bus emulator back to
  its original consumer queue. Use this when a MassTransit consumer faulted
  locally (e.g. AppInstalledHandler failing on a placeholder Slack token) and
  you want to replay the same message without re-triggering the original flow
  (e.g. redoing a real Slack OAuth install). Trigger on phrases like "retry the
  faulted message", "requeue the fault", "replay that faulted event", or when
  investigating why a queue/subscription in the emulator dashboard shows a
  fault but nothing seems to have happened downstream.
---

# Retry a faulted message

`dev/RetryFaulted.cs` is a .NET 10 file-based app (chmod +x) that talks directly to the
local `AlmostServiceBus` emulator (started via `devenv.sh` / `FplBot.AppHost`) using the
official `Azure.Messaging.ServiceBus` SDK — no MassTransit hosting required.

It is fully generic: it does not hardcode any message type, consumer, or queue name.

## What it does

1. Queries the emulator's dashboard API (`http://localhost:15672/api/dashboard/namespaces/default/entities`)
   for topics named `MassTransit/Fault--*--`.
2. For each such topic's subscriptions, tries to receive one message (peek-lock).
3. On the first message found:
   - Extracts the original message's type (`faultMessageTypes`) and payload (`message.message`)
     from the MassTransit `Fault<T>` envelope.
   - Extracts the target queue name from the fault's `sourceAddress` (the consumer that faulted).
   - Completes (removes) the fault message from the fault subscription.
   - Constructs a fresh, correctly-typed MassTransit envelope and sends it to the target queue.
4. If nothing is found, it reports "No faulted messages found." and exits — safe to run
   speculatively any time.

## Running it

```bash
cd dev
./RetryFaulted.cs
# or: dotnet run RetryFaulted.cs
```

Requires the local dev Service Bus emulator to be running (`devenv.sh` — the user runs this
themselves, never start it yourself). No arguments needed.

## Notes

- If the replayed message faults again with the *same* error, the consumer's underlying
  issue hasn't actually been fixed yet (e.g. code change not picked up because the
  consuming service — `EventHandlers` etc. — hasn't been restarted; Rider's "Rerun" button
  does **not** rebuild, only "Run" does).
- The emulator dashboard UI (`http://localhost:15672`) has known display bugs around
  `ForwardTo` subscriptions — a subscription's message-count badge can disagree with what
  clicking into it shows. The dashboard's own REST API
  (`/api/dashboard/namespaces/{ns}/topics/{topic}/messages`) is more reliable for inspecting
  fault content directly if you need to debug the script itself.
- This script is a dev-only tool. It is not part of the FplBot.csproj build and has no
  bearing on production.
