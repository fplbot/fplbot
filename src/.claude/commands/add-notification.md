# Add a new FPL event notification

This skill adds a notification type that gets broadcast to subscribed Slack workspaces and Discord guilds when an FPL event occurs.

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

### 6. Register both consumers — CRITICAL

Open `FplBot/Services/EventHandlers/EventHandlersService.cs` and add both to `ConfigureMassTransit`:

```csharp
cfg.AddConsumer<DiscordYourEventHandler>();
cfg.AddConsumer<SlackYourEventHandler>();
```

### 7. Fan-out: don't do per-channel work here

The handlers above only format once and dispatch — that's fine. If your notification needs per-channel data (that channel's followed league, a per-channel FPL call), it must not happen in this loop. Publish a per-channel command instead and do the work in a second consumer, as `GameweekJustBegan` → `ProcessNewLeagueEntriesForGuildChannel` does. One message per channel isolates a slow or failing league from the rest of the fan-out.

### 8. Formatter

If formatting is non-trivial, put it in `FplBot/Services/EventPublishers/Formatting/` (namespace `FplBot.Formatting`). Builders stay `static` and synchronous, taking already-fetched data as parameters — do the `await` in the handler, not the builder.

### 9. Tests — required, not optional

- Add an **E2E test** in `FplBot.Tests/E2E/` that drives the publisher (the `RecurringAction` / state class), not a hand-constructed event, and asserts the Slack/Discord message that comes out. Use `AppFixture`, set state up via its real flows (`InstallSlackbot()`, `Subscribe(...)`, `AskSlackbot(...)`), and assert on outcomes — no `A.CallTo()` assertions on internals.
- Add a formatter unit test in `FplBot.Tests/UnitTests/Formatting/` only if the formatting is worth pinning down in isolation.

## Verify

```bash
dotnet build --no-incremental src/FplBot/FplBot.csproj
dotnet run --project src/Build -- test
```

Check that a consumer for your event type shows up in the MassTransit endpoint list at startup.
