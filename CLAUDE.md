# FplBot — Claude Context

## What this is

FplBot is a Fantasy Premier League chatbot for Slack and Discord. It monitors FPL gameweek events (goals, injuries, deadline countdowns, price changes, etc.) and broadcasts notifications to subscribed workspaces/guilds.

## Architecture

Single `FplBot.csproj` (monolith) deployed as **four independent Docker containers**, selected at startup via the `--services` flag:

| Service flag | Role |
|---|---|
| `WebApi` | HTTP endpoints: Slack/Discord webhooks, OAuth, slash commands |
| `EventHandlers` | Consumes MassTransit events → posts messages to Slack/Discord |
| `EventPublishers` | Background jobs that poll FPL API and publish events to the bus |
| `SearchIndexer` | Syncs FPL player/league data to Elasticsearch |

Entry point: `Program.cs` → `FplBotApplication.RunAsync(args, services)` in `Hosting/FplBotApplication.cs`.

Each service implements `IFplBotService` and registers its own DI, consumers, and middleware.

## Local dev

```bash
./src/devenv.sh          # starts Redis + Azure Service Bus emulator via Aspire
dotnet run --project src/FplBot  # runs all 4 services together (dev mode)
```

All secrets have safe dev defaults in `appsettings.json`. No real credentials are needed to run locally — in Development, `DevLoggingSlackClient`/`DevLoggingDiscordClient` short-circuit outbound Slack/Discord calls into log lines instead of hitting the real APIs.

### Testing against a real Discord app

The `"dev"` placeholders in `appsettings.json` cannot be replaced with a real bot token/secret —
never commit real credentials, even for a throwaway bot: GitHub's secret scanning partnership with
Discord detects and revokes tokens the moment they're pushed, regardless of intent.

If you need to exercise the real Discord API path (bypassing the dev-logging wrapper, or verifying
real interaction webhooks), use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
instead — they live outside the repo in your user profile and are auto-loaded in Development by
both `WebApplication.CreateBuilder` (WebApi) and `Host.CreateDefaultBuilder` (EventHandlers/
EventPublishers/SearchIndexer), no code changes needed:

```bash
dotnet user-secrets set DISCORD_TOKEN "..." --project src/FplBot
dotnet user-secrets set DISCORD_CLIENT_ID "..." --project src/FplBot
dotnet user-secrets set DISCORD_CLIENT_SECRET "..." --project src/FplBot
dotnet user-secrets set DISCORD_PUBLICKEY "..." --project src/FplBot
dotnet user-secrets set DiscordAppId "..." --project src/FplBot
```

All five values must come from the *same* Discord Application (Developer Portal → your app →
Bot tab for the token, General Information tab for the rest) — they're tied together as one
identity, so mixing values from different apps fails (wrong bot invited, signature verification
failures, etc). There's no API/CLI to create a Discord Application programmatically; it's a
one-time manual step at discord.com/developers/applications.

## MassTransit — the critical pattern

**All cross-service messaging goes through MassTransit with Azure Service Bus.** Do not use direct service calls or MediatR for anything cross-service.

### Message contracts

Defined as C# `record`s in `FplBot/Messaging/`:
- `Messaging/Events/v1/` — things that happened (`GameweekJustBegan`, `FixtureEventsOccured`, etc.)
- `Messaging/Commands/v1/` — instructions to send a message (`PublishToGuildChannel`, `PublishToSlack`, etc.)

Always use `record`, always namespace them under `v1`.

### Consumers

Implement `IConsumer<TMessage>`. A handler can implement multiple message types:

```csharp
public class MyHandler : IConsumer<SomeEvent>, IConsumer<OtherEvent>
{
    public async Task Consume(ConsumeContext<SomeEvent> context)
    {
        // publish a command
        await context.Publish(new PublishToGuildChannel(...));
    }
}
```

**Every consumer must be explicitly registered** in `EventHandlersService.ConfigureMassTransit()`:

```csharp
cfg.AddConsumer<MyHandler>();
```

Forgetting this step means the consumer silently receives nothing — no error is thrown.

### Publishing

| Where | How |
|---|---|
| Inside a consumer | `await context.Publish(new SomeMessage(...))` |
| In a scoped service | inject `IPublishEndpoint` directly |
| In a **singleton** or background job | use `IServiceScopeFactory` — **never** inject `IPublishEndpoint` into a singleton |

```csharp
// Singleton pattern (RecurringActions, State classes)
using var scope = _scopeFactory.CreateScope();
await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(new SomeEvent(...));
```

### Error handling

Faulted messages are globally discarded (`DiscardFaultedMessages()`). There is no dead-letter queue. Messages have a 2-hour TTL on Azure Service Bus.

## Data access

**Redis only** (no SQL, no ORM). Direct `IDatabase` operations via `StackExchange.Redis`.

Key repositories:
- `Data/Slack/SlackTeamRepository.cs` — Slack workspace subscriptions
- `Data/Discord/DiscordGuildRepository.cs` — Discord guild subscriptions
- `Data/Slack/EventSubscription.cs` — enum of all subscribable event types (used by both Slack and Discord)

Redis key convention: `{EntityType}-{id}` (e.g. `TeamId-T12345`, `GuildSubs-{guildId}-Channel-{channelId}`).

## Adding a new notification

See `.claude/commands/add-notification.md` for the full recipe. Summary:
1. Define event record in `Messaging/Events/v1/`
2. Add `EventSubscription` enum value
3. Add publishing in a `RecurringAction` or `State` class
4. Create Discord handler (`Services/EventHandlers/Discord/`)
5. Create Slack handler (`Services/EventHandlers/Slack/`)
6. Register both consumers in `EventHandlersService.ConfigureMassTransit()`
7. Add formatter + tests if needed

## Adding a command handler

See `.claude/commands/add-command-handler.md`.

## Testing

Framework: xUnit + FakeItEasy. Run with:
```bash
dotnet run --project src/Build -- test
# or
dotnet test src
```

- Unit tests: inject fakes via `FplBot.Tests/Helpers/Factory.cs`
- Integration tests: real Redis via `Testcontainers.Redis`
- E2E tests: `FplBot.Tests/E2E/` — spin up an in-memory bus

## Key file locations

```
Hosting/FplBotApplication.cs          — service wiring, MassTransit config
Hosting/IFplBotService.cs             — service plugin interface
Services/EventHandlers/EventHandlersService.cs — consumer registrations
Services/EventPublishers/             — recurring jobs, state machines
Services/WebApi/                      — HTTP endpoints
Messaging/Events/v1/                  — event contracts
Messaging/Commands/v1/                — command contracts
Data/Slack/EventSubscription.cs       — subscription enum (Slack + Discord)
FplBot.csproj                         — single project file, all packages here
Build/Program.cs                      — Bullseye build targets
Dockerfile                            — multi-stage build
src/devenv.sh                         — local dev startup
```

## FPL domain glossary

| Term | Meaning |
|---|---|
| Gameweek (GW) | One round of Premier League fixtures (38 per season) |
| Entry | A single user's FPL team |
| Classic league | A group of entries competing on total points |
| Deadline | Transfer cutoff before a gameweek |
| EventSubscription | Opt-in notification category (goals, injuries, deadlines, etc.) |
| BPS | Bonus Points System — determines who gets bonus points per fixture |
| Chip | Special one-use boost (Triple Captain, Wildcard, Free Hit, Bench Boost) |
