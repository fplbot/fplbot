# Add a new FPL event notification

This skill adds a notification type that gets broadcast to subscribed Slack workspaces, Discord guilds and web push subscribers when an FPL event occurs.

## What you need to know first

- All events flow through MassTransit (Azure Service Bus). A publisher detects something in the FPL API and publishes an event; separate Slack and Discord consumers receive it and post messages.
- Two enums control subscriptions and must stay value-for-value identical, because they are converted by name: `Domain/FplEvent.cs` (domain-facing, used by repositories and handlers) and `Data/EventSubscription.cs` (user-facing, drives the Discord slash-command choices and Slack subscribe parsing).
- Every consumer **must** be manually registered in `EventHandlersService.ConfigureMassTransit()` or it silently receives nothing.
- Handlers use primary constructors and are named `Discord<Name>Handler` / `Slack<Name>Handler` — the platform prefix avoids name clashes, so no `using` aliases.

## Steps

### 1. Define the event contract

Create a C# `record` in `FplBot/Messaging/Events/v1/`. Keep it minimal — only the data consumers need.

```csharp
// FplBot/Messaging/Events/v1/YourEvent.cs
namespace FplBot.Messaging.Contracts.Events.v1;

public record YourEvent(int GameweekId, string SomeData);
```

### 2. Add the subscription value to both enums

`FplBot/Domain/FplEvent.cs`:

```csharp
public enum FplEvent
{
    // ... existing values ...
    YourNewType,
}
```

`FplBot/Data/EventSubscription.cs` — same name, same position:

```csharp
public enum EventSubscription
{
    // ... existing values ...
    YourNewType,
}
```

### 3. Publish the event from a publisher

Find the appropriate publisher in `FplBot/Services/EventPublishers/`. Most events come from either:
- A `RecurringAction` class (polling on a cron schedule)
- A `State` class (`FixtureState`, `LineupState`, `NearDeadLineMonitor`, `MatchDayStatusMonitor`)

In a singleton, always use `IServiceScopeFactory` to resolve `IPublishEndpoint` — never inject `IPublishEndpoint` into a singleton, and never use `IBus`:

```csharp
using var scope = _scopeFactory.CreateScope();
await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
    .Publish(new YourEvent(gameweekId, someData));
```

### 4. Create the Discord handler

Create `FplBot/Services/EventHandlers/Discord/DiscordYourEventHandler.cs`:

```csharp
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordYourEventHandler(IGuildRepository repo, ILogger<DiscordYourEventHandler> logger)
    : IConsumer<YourEvent>
{
    public async Task Consume(ConsumeContext<YourEvent> context)
    {
        var message = context.Message;
        var formatted = Formatter.FormatYourEvent(message);

        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.YourNewType);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "🔔 Your Title", formatted));
        }
    }
}
```

`GetChannelsSubscribedTo` does the filtering — there is no "fetch all installations, then check `HasRegisteredFor`" API.

### 5. Create the Slack handler

Create `FplBot/Services/EventHandlers/Slack/SlackYourEventHandler.cs`. Same shape, with `ISlackTeamRepository` and `PublishToSlack`:

```csharp
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class SlackYourEventHandler(ISlackTeamRepository slackTeamRepo, ILogger<SlackYourEventHandler> logger)
    : IConsumer<YourEvent>
{
    public async Task Consume(ConsumeContext<YourEvent> context)
    {
        var message = context.Message;
        var formatted = Formatter.FormatYourEvent(message);

        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.YourNewType);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToSlack(teamId, channelId, formatted));
        }
    }
}
```

Never post to Slack with `ISlackClientBuilder` from a handler — go through `PublishToSlack` (or `ISlackWorkSpacePublisher`), which owns delivery-failure accounting.

### 6. Decide the web delivery — every enum value ships to the web whether you meant it or not

`FplEvents.SupportedOnWeb` is derived from the enum, so your new value is automatically offered as a checkbox on `/notifications`, enabled by default for new subscribers, and indexed in Redis. Give it a delivery path or opt it out — never neither:

- Deliver: add `IConsumer<YourEvent>` to `FplBot/Services/EventHandlers/Web/WebPushDispatchHandler.cs` and dispatch via `Dispatch` (global events) or `repo.GetFollowingALeague` (league-scoped ones — fan out a per-subscriber command, mirroring `ProcessGameweekFinishedForWebPushSubscriber`/`ProcessGameweekStartedForWebPushSubscriber`, rather than doing the league fetch inline). Build the title/body from the same `Formatter`/`Formatting.Helpers` methods Discord and Slack already call for this event — don't hand-roll new text. The handler class is already registered, so no new registration is needed for this.
- Opt out: exclude the value from `FplEvents.SupportedOnWeb` in `FplBot/Domain/FplEvents.cs`, next to `Taunts`.

Skipping both leaves a checkbox that never delivers anything.

### 7. Register both consumers — CRITICAL

Open `FplBot/Services/EventHandlers/EventHandlersService.cs` and add both to `ConfigureMassTransit`:

```csharp
cfg.AddConsumer<DiscordYourEventHandler>();
cfg.AddConsumer<SlackYourEventHandler>();
```

### 8. Fan-out: don't do per-channel work here

The handlers above only format once and dispatch — that's fine. If your notification needs per-channel data (that channel's followed league, a per-channel FPL call), it must not happen in this loop. Publish a per-channel command instead and do the work in a second consumer, as `GameweekJustBegan` → `ProcessNewLeagueEntriesForGuildChannel` does. One message per channel isolates a slow or failing league from the rest of the fan-out.

### 9. Formatter

If formatting is non-trivial, put it in `FplBot/Services/EventPublishers/Formatting/` (namespace `FplBot.Formatting`). Builders stay `static` and synchronous, taking already-fetched data as parameters — do the `await` in the handler, not the builder.

### 10. Tests — required, not optional

- Add an **E2E test** in `FplBot.Tests/E2E/` that drives the publisher (the `RecurringAction` / state class), not a hand-constructed event, and asserts the Slack/Discord message that comes out. Use `AppFixture`, set state up via its real flows (`InstallSlackbot()`, `Subscribe(...)`, `AskSlackbot(...)`), and assert on outcomes — no `A.CallTo()` assertions on internals.
- Add a formatter unit test in `FplBot.Tests/UnitTests/Formatting/` only if the formatting is worth pinning down in isolation.

### 11. After deploying: push the new Discord slash-command choice

Adding a value to `EventSubscription` changes what the *code* would offer, but Discord's own stored
copy of the `/subscriptions` command's `event` choices doesn't refresh itself — nothing pushes it
automatically on deploy or app startup. Until someone does, the new value is live everywhere
*except* that dropdown, which silently keeps showing the old list.

Once the deploy has gone out (the running app needs the new enum value compiled in first):
1. Go to `/admin` → **Discord slash commands**.
2. Click **"Install to test guild"** first to sanity-check the new choice looks right (guild-scoped,
   shows up within seconds).
3. Click **"Install globally"** for it to reach every guild. Discord can take **up to ~1 hour** to
   propagate a global command update — don't assume it's broken if it's not there immediately.

This is manual because `DiscordSlashCommandsEnsurer` is only ever invoked from those two admin
endpoints (`POST /api/admin/discord/slashcommands/install[-global]`) — there's no hook that calls it
on deploy.

## Verify

```bash
dotnet build --no-incremental src/FplBot/FplBot.csproj
dotnet run --project src/Build -- test
```

Check that a consumer for your event type shows up in the MassTransit endpoint list at startup.
