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
dotnet run --project src/FplBot -- --services All  # runs all 4 services together
```

Seed data for local dev (Slack workspaces, Discord guilds, Elasticsearch docs) lives in
`src/FplBot.AppHost/DevSeederLifecycleHook.cs` — it writes straight to Redis/ES on `aspire` startup.
Add a workspace/guild there; it takes effect on the next devenv restart. The AppHost shares
FplBot's `fplbot-secrets` user-secrets store, so a real token for a seeded workspace goes in
`dotnet user-secrets set DEV_SEED_SLACK_TOKEN "..." --project src/FplBot.AppHost` rather than the file.

All secrets have safe dev defaults in `appsettings.json`. No real credentials are needed to run locally — in Development, `DevLoggingSlackClient`/`DevLoggingDiscordClient` short-circuit outbound Slack/Discord calls into log lines instead of hitting the real APIs.

### Testing against a real Discord app

The `"dev"` placeholders in `appsettings.json` cannot be replaced with a real bot token/secret —
never commit real credentials, even for a throwaway bot: GitHub's secret scanning partnership with
Discord detects and revokes tokens the moment they're pushed, regardless of intent.

If you need to exercise the real Discord API path (bypassing the dev-logging wrapper, or verifying
real interaction webhooks), use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
instead — they live outside the repo in your user profile and are loaded in both Development and
Integration, across all four services, no code changes needed:

```bash
dotnet user-secrets set DISCORD_TOKEN "..." --project src/FplBot
dotnet user-secrets set DISCORD_CLIENT_ID "..." --project src/FplBot
dotnet user-secrets set DISCORD_CLIENT_SECRET "..." --project src/FplBot
dotnet user-secrets set DISCORD_PUBLICKEY "..." --project src/FplBot
dotnet user-secrets set DiscordAppId "..." --project src/FplBot
```

In Development, `DevLoggingSlackClient`/`DevLoggingDiscordClient` short-circuit every outbound
call into a log line — including Discord interaction followups, so a deferred slash command would
sit on "thinking…" forever. Run the **Integration** environment to exercise the real APIs:

```bash
DOTNET_ENVIRONMENT=Integration dotnet run --project src/FplBot -- --services All
```

In Rider, use the **All Services (Integration)** run configuration (there's an `(Integration)`
variant of each single-service config too, in `src/.idea/.../runConfigurations/`).

Integration is local like Development — same user secrets, https on localhost, telemetry to the
Aspire dashboard, `[MachineName]` prefix on outgoing messages — but every integration is live.
Request signature verification is on, since real Slack and Discord sign their webhooks.

In code: `env.IsLocal()` covers both Development and Integration and guards machine conveniences;
plain `env.IsDevelopment()` is reserved for the branches that fake an outbound integration.

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

### Fan-out: never do per-channel work in a global handler

A consumer of a global FPL event (`GameweekJustBegan`, `FixtureEventsOccured`, …) may only
*dispatch*. Any heavy lifting for a `ChannelSubscription` — fetching that channel's league,
calling the FPL API, formatting its message — belongs in a second handler that consumes a
per-channel command:

```csharp
// Global event handler: dispatch only, no I/O per channel
public async Task Consume(ConsumeContext<GameweekJustBegan> context)
{
    foreach (var (guildId, channelId, leagueId) in await repo.GetChannelsFollowingALeague())
        await context.Publish(new ProcessNewLeagueEntriesForGuildChannel(guildId, channelId, (int)leagueId.Value, gameweekId));
}

// Per-channel handler: the actual work, one message per channel
public async Task Consume(ConsumeContext<ProcessNewLeagueEntriesForGuildChannel> context)
{
    var league = await NewLeagueEntries.Fetch(...);
    ...
}
```

Existing pairs follow this shape: `GameweekJustBegan` → `ProcessGameweekStartedForGuildChannel`,
`GameweekJustBegan` → `ProcessNewLeagueEntriesForGuildChannel` / `…ForSlackChannel`.

Why it matters: a global handler doing the work processes every channel serially inside one
message, so one slow or failing league stalls or kills the rest, and the whole fan-out retries
or is discarded as a unit. Splitting it gives each channel its own message, its own failure,
and lets the broker spread the load.

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

Faulted messages land in MassTransit's per-consumer `_error` queue. Inspect and act on them from
the admin Errors dashboard (`Services/WebApi/Endpoints/Api/Admin/AdminErrorEndpoints.cs`), which
exposes list / retry / retry-all / discard / purge per queue.

Messages have a 2-hour TTL on Azure Service Bus, so anything older than that is gone regardless.

## Domain

`FplBot/Domain/` holds the platform-agnostic model shared by Slack and Discord:

| Type | Role |
|---|---|
| `Installation` | one Slack workspace / Discord guild, with its channel subscriptions |
| `ChannelSubscription` | one channel: which `FplEvent`s it gets, which league it follows |
| `EventCollection` | the set of `FplEvent`s a channel subscribes to |
| `ClassicLeagueId` | validated league id value type |
| `FplEvent` | enum of subscribable event types |

Domain types have **private constructors and named static factories** — never public constructors
or object initializers. Loading from storage uses a `FromStorage` factory; mappers must not call
behaviour methods like `Install`/`Follow`/`Subscribe` to rebuild state.

`FplBot/ApplicationServices/` composes domain calls for a given trigger context (e.g.
`AdminUninstallSlackWorkspace`). Application services never publish to the bus — the caller does.
Distinct trigger contexts get distinct named classes, not a flag on a shared one.

A second enum `Data/EventSubscription.cs` carries the **same values under the same names**. It is
the user-facing list: the Discord slash-command choices are generated from
`Enum.GetNames<EventSubscription>()`, and the Slack/Discord subscribe commands parse user input into
it. The two are converted by name (`Enum.Parse<FplEvent>(e.ToString())`), so the enums must stay
value-for-value identical — **adding a notification means adding the value to both**. It also backs
the `StatType` → subscription mapping helpers under `Services/EventHandlers/*/Helpers/`.

Everything past the command boundary — repositories, `ChannelSubscription`, event handlers — uses
`Domain/FplEvent.cs`.

## Data access

**Redis only** (no SQL, no ORM). Direct `IDatabase` operations via `StackExchange.Redis`.

Both platforms implement the same `Data/IDomainRepository.cs`:
- `Data/Slack/SlackTeamRepository.cs` (`ISlackTeamRepository`) — Slack workspaces
- `Data/Discord/DiscordGuildRepository.cs` (`IGuildRepository`) — Discord guilds

`ISlackTeamRepository` and `IGuildRepository` add no members of their own; they exist so DI can tell
the two instances apart. To find who should receive a notification, use
`GetChannelsSubscribedTo(params FplEvent[])`, which returns `(InstallationId, ChannelId)` pairs —
there is no "get everything, then filter in the handler" API.

Redis key convention: `{EntityType}-{id}` (e.g. `TeamId-T12345`, `GuildSubs-{guildId}-Channel-{channelId}`).

## Adding a new notification

See `src/.claude/commands/add-notification.md` for the full recipe. Summary:
1. Define event record in `Messaging/Events/v1/`
2. Add the value to both `Domain/FplEvent.cs` and `Data/EventSubscription.cs`
3. Add publishing in a `RecurringAction` or `State` class
4. Create Discord handler (`Services/EventHandlers/Discord/Discord<Name>Handler.cs`)
5. Create Slack handler (`Services/EventHandlers/Slack/Slack<Name>Handler.cs`)
6. Register both consumers in `EventHandlersService.ConfigureMassTransit()`
7. Add an E2E test (`FplBot.Tests/E2E/`), plus a formatter unit test if the formatting is non-trivial

## Adding a command handler

Chat commands are two-stage since #459: the WebApi mention/interaction handler only publishes a
`Process<Name>Command`, and a consumer in `Services/EventHandlers/{Slack,Discord}/Commands/` does
the work and posts the reply.

See `src/.claude/commands/add-command-handler.md`.

## Testing

Framework: xUnit + FakeItEasy. Run with:
```bash
dotnet run --project src/Build -- test
# or
dotnet test src
```

Full rules: the `write-test` skill (`.claude/skills/write-test/SKILL.md`). In short:

- Test from the highest possible entry point. Default to an E2E test in `FplBot.Tests/E2E/` via
  `AppFixture` — real test host, real Redis and Azure Service Bus emulator via Testcontainers.
- Set up state through the real flows `AppFixture` exposes (`InstallSlackbot()`, `Subscribe(...)`,
  `AskSlackbot(...)`), not by seeding repositories directly.
- Mock only the external integrations: FPL APIs, Slack, Discord. Use real infra for everything we
  already run in Docker.
- Assert on outcomes, never on internals — no `A.CallTo()` assertions on internal logic in E2E tests.
- `FplBot.Tests/UnitTests/` is for helpers, formatters and static methods only. If the unit depends
  on another component, move the test up to E2E instead.
- Any feature change gets a test: E2E for consumers / recurring jobs / state machines, an
  `E2E/ApiEndpoints/` test for HTTP-facing endpoint handlers.

## Key file locations

```
Hosting/FplBotApplication.cs          — service wiring, MassTransit config
Hosting/IFplBotService.cs             — service plugin interface
Services/EventHandlers/EventHandlersService.cs — consumer registrations
Services/EventPublishers/             — recurring jobs, state machines
Services/EventPublishers/Formatting/  — message formatters (namespace FplBot.Formatting)
Services/EventHandlers/*/Commands/    — chat command consumers (the work half)
Services/WebApi/                      — HTTP endpoints, incl. the mention/interaction handlers
Messaging/Events/v1/                  — event contracts
Messaging/Commands/v1/                — command contracts
Domain/                               — Installation, ChannelSubscription, FplEvent
ApplicationServices/                  — trigger-context composition over Domain
Data/IDomainRepository.cs             — the repository contract both platforms implement
Domain/FplEvent.cs                    — subscription enum (Slack + Discord)
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
| FplEvent | Opt-in notification category (goals, injuries, deadlines, etc.) |
| BPS | Bonus Points System — determines who gets bonus points per fixture |
| Chip | Special one-use boost (Triple Captain, Wildcard, Free Hit, Bench Boost) |
