# Add a new FPL event notification

This skill adds a new notification type that gets broadcast to subscribed Slack workspaces and Discord guilds when an FPL event occurs.

## What you need to know first

- All events flow through MassTransit (Azure Service Bus). The publisher detects something in the FPL API and publishes an event; separate consumers for Slack and Discord receive it and post messages.
- `EventSubscription` enum (in `FplBot/Data/Slack/EventSubscription.cs`) controls whether a team/guild receives the notification. It's used by both platforms despite the `Slack` namespace.
- Every consumer **must** be manually registered in `EventHandlersService.ConfigureMassTransit()` or it silently receives nothing.

## Steps

### 1. Define the event contract

Create a C# `record` in `FplBot/Messaging/Events/v1/`. Keep it minimal — only the data consumers need.

```csharp
// FplBot/Messaging/Events/v1/YourEvent.cs
namespace FplBot.Messaging.Contracts.Events.v1;

public record YourEvent(int GameweekId, string SomeData);
```

### 2. Add an EventSubscription value

Open `FplBot/Data/Slack/EventSubscription.cs` and add your new value to the enum.

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

In a singleton, always use `IServiceScopeFactory` to resolve `IPublishEndpoint`:

```csharp
// In a singleton state class
using var scope = _scopeFactory.CreateScope();
await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
    .Publish(new YourEvent(gameweekId, someData));
```

### 4. Create the Discord handler

Create `FplBot/Services/EventHandlers/Discord/YourEventHandler.cs`:

```csharp
using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class YourEventHandler : IConsumer<YourEvent>
{
    private readonly IGuildRepository _repo;
    private readonly ILogger<YourEventHandler> _logger;

    public YourEventHandler(IGuildRepository repo, ILogger<YourEventHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<YourEvent> context)
    {
        var message = context.Message;
        var guildSubs = await _repo.GetAllGuildSubscriptions();
        var formatted = FormatMessage(message);

        foreach (var guild in guildSubs)
        {
            if (guild.Subscriptions.ContainsSubscriptionFor(EventSubscription.YourNewType) 
                && !string.IsNullOrEmpty(formatted))
            {
                await context.Publish(new PublishRichToGuildChannel(
                    guild.GuildId, guild.ChannelId,
                    "🔔 Your Title", formatted));
            }
        }
    }

    private string FormatMessage(YourEvent e) => $"Something happened in GW{e.GameweekId}: {e.SomeData}";
}
```

### 5. Create the Slack handler

Create `FplBot/Services/EventHandlers/Slack/YourEventHandler.cs`. The pattern mirrors the Discord handler but uses `ISlackTeamRepository` and publishes `PublishToSlack`:

```csharp
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

internal class YourEventHandler : IConsumer<YourEvent>
{
    private readonly ISlackTeamRepository _teamRepo;
    private readonly ILogger<YourEventHandler> _logger;

    public YourEventHandler(ISlackTeamRepository teamRepo, ILogger<YourEventHandler> logger)
    {
        _teamRepo = teamRepo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<YourEvent> context)
    {
        var message = context.Message;
        var teams = await _teamRepo.GetAllTeams();
        var formatted = $"Something happened in GW{message.GameweekId}: {message.SomeData}";

        foreach (var team in teams)
        {
            if (team.HasRegisteredFor(EventSubscription.YourNewType) && !string.IsNullOrEmpty(formatted))
            {
                await context.Publish(new PublishToSlack(team.TeamId!, team.FplBotSlackChannel!, formatted));
            }
        }
    }
}
```

### 6. Register both consumers — CRITICAL

Open `FplBot/Services/EventHandlers/EventHandlersService.cs` and add both consumers to `ConfigureMassTransit`:

```csharp
public void ConfigureMassTransit(IBusRegistrationConfigurator cfg)
{
    // ... existing registrations ...
    cfg.AddConsumer<DiscordYourEventHandler>();  // add using alias at top if names clash
    cfg.AddConsumer<SlackYourEventHandler>();
}
```

If the Discord and Slack handler class names are identical, add `using` aliases at the top of the file (follow the existing pattern):

```csharp
using DiscordYourEventHandler = FplBot.EventHandlers.Discord.YourEventHandler;
using SlackYourEventHandler = FplBot.EventHandlers.Slack.YourEventHandler;
```

### 7. Add formatter and tests (if needed)

If the message formatting is non-trivial, extract it to `FplBot/Formatting/` and add unit tests in `FplBot.Tests/Formatting/`. Look at existing formatters (e.g. `Formatting/FixtureStats/`) for the pattern.

### 8. Update subscription UI (if opt-in)

If the new event type should be user-configurable (most are):
- Discord: update the subscribe/unsubscribe slash command handler in `Services/WebApi/Discord/`
- Slack: update the subscription management in `Services/WebApi/Slack/`

## Verify

```bash
dotnet build src/FplBot
dotnet test src
```

Check that a consumer for your event type shows up in the MassTransit endpoint list at startup (look for your handler class name in the logs).
