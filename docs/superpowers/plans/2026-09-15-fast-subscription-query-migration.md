# Fast Subscription Query Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the `GetAllInstallations()` + in-memory `GetSubscriptionsTo(...)` scan (which hydrates every installation, every channel, every subscription just to throw most of it away) with the Redis-backed `GetChannelsSubscribedTo(params FplEvent[])` union-index query — already used by `SlackPriceChangeHandler`/`DiscordPriceChangeHandler` — across every remaining Slack and Discord event handler.

**Architecture:** `GetChannelsSubscribedTo` does one `SETUNION` over per-event Redis Sets (`SlackEventIndex-{event}` / `GuildEventIndex-{event}`) and returns exactly the `(InstallationId, ChannelId)` pairs subscribed to the given event(s) — O(subscribed channels), not O(all installations × all channels). A handful of handlers also need per-channel data the tuple doesn't carry (`FollowedLeagueId`, or the full multi-event `ChannelSubscription`); for those we add one new repository method, `GetChannelSubscription(installationId, channelId)`, which does a single direct `HGET` by an already-known key — no scan, no data migration, since the underlying Redis hash fields (`leagueId`/`leagueid`) already exist today.

**Tech Stack:** C#/.NET, MassTransit consumers, StackExchange.Redis, xUnit E2E tests via `AppFixture` (real Redis Testcontainer, real in-memory bus).

**Spec:** No separate spec doc — this plan was scoped directly from a codebase investigation (see conversation). Key findings baked into every task below:
- `GetChannelsSubscribedTo` already exists on `IDomainRepository`, implemented identically in `SlackTeamRepository.cs:120-125` and `DiscordGuildRepository.cs:89-94`.
- Every handler below is currently on the slow path: `GetAllInstallations()` (full hash scan + per-channel hydration for every installation) followed by an in-memory filter (`Installation.GetSubscriptionsTo(FplEvent)` at `Domain/Installation.cs:104-107`, or a custom predicate).
- All touched handlers have existing E2E test coverage via `AppFixture` (real Redis, real bus) — no unit-test/fakes layer exists for these handlers, so **no new tests are required**; each task's verification step is "run the existing E2E test file(s) and confirm green."

## Global Constraints

- Every task must build with **zero warnings** (`dotnet run --project src/Build -- test` — see CLAUDE.md); do not use `--no-verify` or skip hooks.
- Never inject `IPublishEndpoint` into a singleton — not applicable here (all touched classes are scoped MassTransit consumers), but do not introduce that anti-pattern while editing.
- Preserve every handler's existing message-publishing semantics (same command types, same constructor args) unless a task explicitly calls out an intentional behavior change (two tasks do: Task 9 and Task 11 fix a genuine "publish to everyone, filter later" waste — see their Behavior Note).
- Do not touch `FplBot.Domain` entity methods (`Installation`, `ChannelSubscription`) — this migration is entirely in the repository layer and the consumer layer.
- Commit after each task passes its tests.

---

## Task 1: Add `GetChannelSubscription` to the repository contract

**Files:**
- Modify: `src/FplBot/Data/IDomainRepository.cs`
- Modify: `src/FplBot/Data/Slack/SlackTeamRepository.cs`
- Modify: `src/FplBot/Data/Discord/DiscordGuildRepository.cs`
- Test: `src/FplBot.Tests/E2E/Slack/InstallationTests.cs`, `src/FplBot.Tests/E2E/Discord/InstallationTests.cs` (extend, don't replace)

**Interfaces:**
- Produces: `Task<ChannelSubscription?> GetChannelSubscription(string installationId, string channelId)` — added to `IDomainRepository`, implemented by both repos. Returns `null` if the channel has no subscription (deleted, or never existed). Later tasks (7, 10, 11) call this to fetch `FollowedLeagueId` / full event membership for one already-known channel, without a scan.

- [ ] **Step 1: Add the method signature to the shared interface**

In `src/FplBot/Data/IDomainRepository.cs`, add below `GetChannelsSubscribedTo`:

```csharp
    Task<IEnumerable<(string InstallationId, string ChannelId)>> GetChannelsSubscribedTo(params FplEvent[] fplEvents);
    Task<ChannelSubscription?> GetChannelSubscription(string installationId, string channelId);
```

- [ ] **Step 2: Implement in `SlackTeamRepository`, extracting the shared per-channel read**

In `src/FplBot/Data/Slack/SlackTeamRepository.cs`, replace the body of `GetChannelSubscriptions(string teamId)` (lines 250-266) and add a new private helper plus the public method:

```csharp
    public Task<ChannelSubscription?> GetChannelSubscription(string installationId, string channelId) =>
        ReadChannelSubscription(installationId, channelId);

    // Reads the exact set of channel ids this team has saved, then fetches each channel's hash by
    // its exact key. Deliberately avoids a KEYS pattern scan on "SlackChannelSub-{teamId}-*": that
    // glob also matches OTHER teams whose id happens to start with this team's id plus a dash
    // (e.g. scanning for "DEV-SLACK" would also match "DEV-SLACK-2", "DEV-SLACK-BARE", ...).
    private async Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string teamId)
    {
        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(teamId));
        var result = new List<ChannelSubscription>();

        foreach (var channelIdValue in channelIds)
        {
            var sub = await ReadChannelSubscription(teamId, channelIdValue.ToString()!);
            if (sub is not null)
            {
                result.Add(sub);
            }
        }

        return result;
    }

    private async Task<ChannelSubscription?> ReadChannelSubscription(string teamId, string channelId)
    {
        var fetched = await _db.HashGetAsync(FromTeamAndChannelToChannelSubKey(teamId, channelId), [_channelSubChannelIdField, _channelSubLeagueIdField, _channelSubSubscriptionsField]);
        if (!fetched[0].HasValue)
        {
            return null;
        }

        int? leagueId = fetched[1].HasValue ? int.Parse(fetched[1]!) : null;
        var subs = GetSubscriptions(teamId, fetched[2]);
        var domainLeagueId = leagueId is { } id ? new ClassicLeagueId(id) : null;
        return ChannelSubscription.Load(channelId, domainLeagueId, subs.Select(ToDomainEvent));
    }
```

- [ ] **Step 3: Implement in `DiscordGuildRepository`, same extraction**

In `src/FplBot/Data/Discord/DiscordGuildRepository.cs`, replace the body of `GetChannelSubscriptions(string guildId)` (lines 181-196) and add the helper plus public method:

```csharp
    public Task<ChannelSubscription?> GetChannelSubscription(string installationId, string channelId) =>
        ReadChannelSubscription(installationId, channelId);

    private async Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string guildId)
    {
        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(guildId));
        var result = new List<ChannelSubscription>();
        foreach (var channelIdValue in channelIds)
        {
            var sub = await ReadChannelSubscription(guildId, channelIdValue.ToString()!);
            if (sub is not null)
            {
                result.Add(sub);
            }
        }

        return result;
    }

    private async Task<ChannelSubscription?> ReadChannelSubscription(string guildId, string channelId)
    {
        var fetched = await _db.HashGetAsync(FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId), [_channelIdField, _leagueIdField, _subscriptionsField]);
        if (!fetched[0].HasValue)
        {
            return null;
        }

        var leagueId = fetched[1].HasValue ? (int?)fetched[1] : null;
        var subs = ParseSubscriptionString(fetched[2].ToString(), " ");
        var domainLeagueId = leagueId is { } id ? new ClassicLeagueId(id) : null;
        return ChannelSubscription.Load(channelId, domainLeagueId, subs.Select(ToDomainEvent));
    }
```

- [ ] **Step 4: Add one direct assertion of the new method to each platform's installation test**

In `src/FplBot.Tests/E2E/Slack/InstallationTests.cs`, add a test that seeds an installation with a followed league (via `fixture.SeedInstallation(...)` or `fixture.InstallSlackbot()` + `fixture.Subscribe(...)`), then calls `fixture.Services.GetRequiredService<ISlackTeamRepository>().GetChannelSubscription(teamId, channelId)` and asserts the returned `ChannelSubscription.FollowedLeagueId` matches, and that a nonexistent `channelId` returns `null`. Mirror the same in `src/FplBot.Tests/E2E/Discord/InstallationTests.cs` using `fixture.GuildRepo`.

- [ ] **Step 5: Build and run**

Run: `dotnet run --project src/Build -- test`
Expected: build succeeds with zero warnings; new tests pass; no existing test regresses (this task only adds code paths, doesn't change any handler yet).

- [ ] **Step 6: Commit**

```bash
git add src/FplBot/Data/IDomainRepository.cs src/FplBot/Data/Slack/SlackTeamRepository.cs src/FplBot/Data/Discord/DiscordGuildRepository.cs src/FplBot.Tests/E2E/Slack/InstallationTests.cs src/FplBot.Tests/E2E/Discord/InstallationTests.cs
git commit -m "Add direct per-channel subscription lookup to IDomainRepository"
```

---

## Task 2: Migrate injury-update handlers (Slack + Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackInjuryUpdateHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordInjuryUpdateHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/SlackInjuriesEventHandlerE2ETests.cs`, `src/FplBot.Tests/E2E/Discord/DiscordInjuryUpdateHandlerTests.cs`

**Interfaces:**
- Consumes: `ISlackTeamRepository.GetChannelsSubscribedTo` / `IGuildRepository.GetChannelsSubscribedTo` (Task 1's sibling, already existed pre-plan).

- [ ] **Step 1: Rewrite `SlackInjuryUpdateHandler.Consume`**

Replace lines 22-29 of `SlackInjuryUpdateHandler.cs`:

```csharp
            var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.InjuryUpdates);
            foreach (var (teamId, channelId) in subscribedChannels)
            {
                await context.Publish(new PublishToSlack(teamId, channelId, formatted));
            }
```

- [ ] **Step 2: Rewrite `DiscordInjuryUpdateHandler.Consume`**

Replace lines 22-29 of `DiscordInjuryUpdateHandler.cs`:

```csharp
            var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.InjuryUpdates);
            foreach (var (teamId, channelId) in subscribedChannels)
            {
                await context.Publish(new PublishRichToGuildChannel(teamId, channelId, "ℹ️ Injury update", formatted));
            }
```

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `SlackInjuriesEventHandlerE2ETests` and `DiscordInjuryUpdateHandlerTests` (and `PlayerEventPublishingE2ETests`, which also covers the Slack injury path) pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackInjuryUpdateHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordInjuryUpdateHandler.cs
git commit -m "Use fast subscription query in injury-update handlers"
```

---

## Task 3: Migrate fixture-fulltime handlers (Slack + Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackFixtureFulltimeHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordFixtureFulltimeHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/FixtureEventE2ETests.cs`, `src/FplBot.Tests/E2E/Discord/DiscordFixtureFulltimeHandlerTests.cs`

- [ ] **Step 1: Rewrite `SlackFixtureFulltimeHandler.Consume<FixtureFinished>`**

Replace lines 23-49 of `SlackFixtureFulltimeHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<FixtureFinished> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling fixture full time");
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.FixtureFullTime);
        var settings = await settingsClient.GetGlobalSettings();
        var fixtures = await fixtureClient.GetFixtures() ?? [];
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId);
        if (fplfixture == null)
        {
            logger.LogWarning("Could not find fixture {FixtureId} in FPL API", message.FixtureId);
            return;
        }
        var liveItems = fplfixture.Event.HasValue
            ? await liveClient.GetLiveItems(fplfixture.Event.Value, isOngoingGameweek: true)
            : null;
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings?.Teams ?? [], settings?.Players ?? [], fplfixture, liveItems);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);

        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishFulltimeMessageToSlackWorkspace(teamId, channelId, title, threadMessage));
        }
    }
```

- [ ] **Step 2: Rewrite `DiscordFixtureFulltimeHandler.Consume<FixtureFinished>`**

Replace lines 22-47 of `DiscordFixtureFulltimeHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<FixtureFinished> context)
    {
        var message = context.Message;
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.FixtureFullTime);
        var settings = await settingsClient.GetGlobalSettings();
        var fixtures = await fixtureClient.GetFixtures() ?? [];
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId);
        if (fplfixture == null)
        {
            _logger.LogWarning("Could not find fixture {FixtureId} in FPL API", message.FixtureId);
            return;
        }
        var liveItems = fplfixture.Event.HasValue
            ? await liveClient.GetLiveItems(fplfixture.Event.Value, isOngoingGameweek: true)
            : null;
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings?.Teams ?? [], settings?.Players ?? [], fplfixture, liveItems);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishRichToGuildChannel(guildId, channelId, $"ℹ️ {title}", $"{threadMessage}"));
        }
    }
```

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `FixtureEventE2ETests` and `DiscordFixtureFulltimeHandlerTests` pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackFixtureFulltimeHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordFixtureFulltimeHandler.cs
git commit -m "Use fast subscription query in fixture-fulltime handlers"
```

---

## Task 4: Migrate fixture-removed handlers (Slack + Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackFixtureRemovedHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordFixtureRemovedHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/LineupEventPublishingE2ETests.cs` (`WhenFixtureIsRemoved_EmitsFixtureRemoved`), `src/FplBot.Tests/E2E/Discord/DiscordFixtureRemovedHandlerTests.cs`

- [ ] **Step 1: Rewrite `SlackFixtureRemovedHandler.Consume`**

Replace lines 14-29 of `SlackFixtureRemovedHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<FixtureRemovedFromGameweek> context)
    {
        var message = context.Message;
        logger.LogInformation("Fixture removed from gameweek {Message}", message);

        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.FixtureRemovedFromGameweek);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            var fixture = $"{message.RemovedFixture.Home.Name}-{message.RemovedFixture.Away.Name}";
            var msg = $"❌ *Fixture off!*\n {fixture} has been removed from gameweek {message.Gameweek}!";
            await context.Publish(new PublishToSlack(teamId, channelId, msg));
        }
    }
```

- [ ] **Step 2: Rewrite `DiscordFixtureRemovedHandler.Consume`**

Replace lines 14-33 of `DiscordFixtureRemovedHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<FixtureRemovedFromGameweek> context)
    {
        var message = context.Message;
        logger.LogInformation("Fixture removed from gameweek {Message}", message);
        var subscribedChannels = await guildRepo.GetChannelsSubscribedTo(FplEvent.FixtureRemovedFromGameweek);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            var formattedMsg = new PublishRichToGuildChannel(guildId,
                channelId,
                "❌ Fixture off!",
                $"{message.RemovedFixture.Home.Name}-{message.RemovedFixture.Away.Name}" +
                $" has been removed from gameweek {message.Gameweek}!");
            await context.Publish(formattedMsg);
        }
    }
```

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `LineupEventPublishingE2ETests.WhenFixtureIsRemoved_EmitsFixtureRemoved` and `DiscordFixtureRemovedHandlerTests` pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackFixtureRemovedHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordFixtureRemovedHandler.cs
git commit -m "Use fast subscription query in fixture-removed handlers"
```

---

## Task 5: Migrate lineup-ready handlers (Slack + Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackLineupReadyHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordLineupReadyHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/LineupEventPublishingE2ETests.cs`, `src/FplBot.Tests/E2E/Discord/DiscordLineupReadyHandlerTests.cs`

- [ ] **Step 1: Rewrite `SlackLineupReadyHandler.Consume<LineupReady>`**

Replace lines 17-30 of `SlackLineupReadyHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<LineupReady> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling new lineups");
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.Lineups);

        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishLineupsToSlackWorkspace(teamId, channelId, message.Lineup));
        }
    }
```

- [ ] **Step 2: Rewrite `DiscordLineupReadyHandler.Consume`**

Replace lines 12-27 of `DiscordLineupReadyHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<LineupReady> context)
    {
        var message = context.Message;
        var subscribedChannels = await guildRepository.GetChannelsSubscribedTo(FplEvent.Lineups);
        var lineups = message.Lineup;
        var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* ";
        var formattedLineup = Formatter.FormatLineup(lineups);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishRichToGuildChannel(guildId, channelId, $"ℹ️ {firstMessage}", $"{formattedLineup}"));
        }
    }
```

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `LineupEventPublishingE2ETests` and `DiscordLineupReadyHandlerTests` pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackLineupReadyHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordLineupReadyHandler.cs
git commit -m "Use fast subscription query in lineup-ready handlers"
```

---

## Task 6: Migrate new-player handlers (Slack + Discord, both consumers each)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackNewPlayerHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordNewPlayersHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/PlayerEventPublishingE2ETests.cs`, `src/FplBot.Tests/E2E/Discord/DiscordNewPlayersHandlerTests.cs`

- [ ] **Step 1: Rewrite both consumers in `SlackNewPlayerHandler.cs`**

Replace lines 14-51:

```csharp
    public async Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.NewPlayers.Count()} new players");
        var filtered = notification.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var formatted = Formatter.FormatNewPlayers(filtered);
            var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.NewPlayers);

            foreach (var (teamId, channelId) in subscribedChannels)
            {
                await context.Publish(new PublishToSlack(teamId, channelId, formatted));
            }
        }
        else
        {
            logger.LogInformation("All new players irrelevant, so not sending any notification");
        }
    }

    public async Task Consume(ConsumeContext<PremiershipPlayerTransferred> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.Transfers.Count()} new transfers");
        var formatted = Formatter.FormatTransferredPlayers(notification.Transfers);
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.NewPlayers);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToSlack(teamId, channelId, formatted));
        }
    }
```

- [ ] **Step 2: Rewrite both consumers in `DiscordNewPlayersHandler.cs`**

Replace lines 14-59:

```csharp
    public async Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.NewPlayers.Count()} new players");

        var filtered = message.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.NewPlayers);
            var formatted = Formatter.FormatNewPlayers(filtered);

            foreach (var (guildId, channelId) in subscribedChannels)
            {
                if (!string.IsNullOrEmpty(formatted))
                {
                    await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "ℹ️ New players", formatted));
                }
            }
        }
        else
        {
            logger.LogInformation("All new players irrelevant, so not sending any notification");
        }
    }

    public async Task Consume(ConsumeContext<PremiershipPlayerTransferred> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.Transfers.Count()} new transfers");
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.NewPlayers);
        var formatted = Formatter.FormatTransferredPlayers(message.Transfers, includeheader:false);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            if (!string.IsNullOrEmpty(formatted))
            {
                await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "🔄️ Transfer!", formatted));
            }
        }
    }
```

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `PlayerEventPublishingE2ETests` and `DiscordNewPlayersHandlerTests` pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackNewPlayerHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordNewPlayersHandler.cs
git commit -m "Use fast subscription query in new-player handlers"
```

---

## Task 7: Migrate near-deadline handlers (Slack + Discord, both consumers each)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackNearDeadlineHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordNearDeadlineHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/NearDeadlineEventPublishingE2ETests.cs`, `src/FplBot.Tests/E2E/Discord/DiscordNearDeadlineHandlerTests.cs`

- [ ] **Step 1: Rewrite the two deadline consumers in `SlackNearDeadlineHandler.cs`**

Replace lines 24-54 (leave `Consume<PublishDeadlineNotificationToSlackWorkspace>` at lines 56+ untouched — it's a single-workspace publish, not a scan):

```csharp
    public async Task Consume(ConsumeContext<OneHourToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            var text = $"<!channel> ⏳ Gameweek {message.GameweekNearingDeadline.Id} deadline in 60 minutes!";
            var command = new PublishToSlack(teamId, channelId, text);
            await context.Publish(command);
        }
    }

    public async Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 24h to (gw{message.GameweekNearingDeadline.Id}) deadline");

        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            var command = new PublishDeadlineNotificationToSlackWorkspace(teamId, channelId, message.GameweekNearingDeadline);
            await context.Publish(command);
        }
    }
```

- [ ] **Step 2: Rewrite both consumers in `DiscordNearDeadlineHandler.cs`**

Replace lines 14-43:

```csharp
    public async Task Consume(ConsumeContext<OneHourToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        var text = $"😱 Gameweek {message.GameweekNearingDeadline.Id} deadline in 60 minutes! @here";
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToGuildChannel(guildId, channelId, text));
        }
    }

    public async Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 24 hours to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        var text = $"⏳Gameweek {message.GameweekNearingDeadline.Id} deadline in 24 hours!";
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToGuildChannel(guildId, channelId, $"{text}"));
        }
    }
```

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `NearDeadlineEventPublishingE2ETests` and `DiscordNearDeadlineHandlerTests` pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackNearDeadlineHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordNearDeadlineHandler.cs
git commit -m "Use fast subscription query in near-deadline handlers"
```

---

## Task 8: Migrate `BroadcastHandler` (Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Discord/BroadcastHandler.cs`
- Test: `src/FplBot.Tests/E2E/Discord/BroadcastHandlerTests.cs`

**Behavior note:** the per-channel "Did not pass filter" debug log line is dropped since non-matching channels are no longer enumerated at all — this was diagnostic-only and not asserted by any test.

- [ ] **Step 1: Rewrite `BroadcastHandler.Consume`, removing `SendToInstallation`/`PassesBroadcastFilter`**

Replace the entire body of the class (lines 10-56):

```csharp
    public async Task Consume(ConsumeContext<BroadcastToDiscord> context)
    {
        var message = context.Message;
        logger.LogInformation("HANDLING BROADCAST OF {Message} TO DISCORD USING filter {ChannelFilter}", message.Message, message.Filter);

        if (message.Filter == ChannelFilter.NotSet)
        {
            logger.LogWarning("NOT BROADCASTING THE MESSAGE. Filter was {ChannelFilter}", message.Filter);
            return;
        }

        var devOnly = message.Filter is
            ChannelFilter.AllChannelsDevServer or
            ChannelFilter.OnlyChannelsFollowingALeagueDevServer;

        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.Captains, FplEvent.Transfers, FplEvent.Standings);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            if (!devOnly || guildId == "1546966580007542937")
            {
                logger.LogInformation("Sending message to {GuildId} {ChannelId}", guildId, channelId);
                await context.Publish(new PublishToGuildChannel(guildId, channelId, message.Message));
            }
        }
    }
```

- [ ] **Step 2: Remove the now-unused `using FplBot.Domain;` if no other symbol in the file needs it**

Check the file after Step 1 — `Installation` and `ChannelSubscription` are no longer referenced, so remove `using FplBot.Domain;` from the top of `BroadcastHandler.cs` to avoid an unused-using warning.

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `BroadcastHandlerTests` passes unchanged (build must be zero-warning).

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Discord/BroadcastHandler.cs
git commit -m "Use fast subscription query in BroadcastHandler"
```

---

## Task 9: Migrate fixture-events handlers (Slack + Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackFixtureEventsHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordFixtureEventsHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/FixtureEventE2ETests.cs`, `src/FplBot.Tests/E2E/Discord/DiscordFixtureEventsHandlerTests.cs`

**Behavior note (Discord only):** today `DiscordFixtureEventsHandler.Consume<FixtureEventsOccured>` publishes `PublishFixtureEventsToGuild` to **every channel of every installation**, unconditionally — there is no upfront filter at all (unlike the Slack sibling, which already filters at the installation level via `HasChannelSubscribedToFixtureStats`). The downstream `Consume<PublishFixtureEventsToGuild>` already no-ops for channels not subscribed to any of the four fixture-stat events (`ChannelHasStat` gates `GameweekEventsFormatter.FormatNewFixtureEvents`, which yields no messages), so no *visible* Discord message changes — but the bus traffic and per-channel `FindInstallationByTeamId` calls for irrelevant channels are eliminated. Run `DiscordFixtureEventsHandlerTests` closely to confirm no test relied on the old broadcast-then-filter shape.

- [ ] **Step 1: Rewrite `SlackFixtureEventsHandler.Consume<FixtureEventsOccured>`, drop `HasChannelSubscribedToFixtureStats`**

Replace lines 27-48 of `SlackFixtureEventsHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.FixtureEvents.Count} new fixture events");
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FixtureStatEvents);
        var subscribedInstallationIds = subscribedChannels.Select(c => c.InstallationId).Distinct();

        foreach (var installationId in subscribedInstallationIds)
        {
            await context.Publish(new PublishFixtureEventsToSlackWorkspace(installationId, message.FixtureEvents), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
        }
    }

    private static readonly FplEvent[] FixtureStatEvents =
    [
        FplEvent.FixtureGoals,
        FplEvent.FixtureAssists,
        FplEvent.FixtureCards,
        FplEvent.FixturePenaltyMisses
    ];
```

(This removes the old `HasChannelSubscribedToFixtureStats(Installation installation)` private method entirely — it's replaced by passing `FixtureStatEvents` straight into `GetChannelsSubscribedTo`.)

- [ ] **Step 2: Rewrite `DiscordFixtureEventsHandler.Consume<FixtureEventsOccured>`**

Replace lines 22-35 of `DiscordFixtureEventsHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.FixtureEvents.Count} new fixture events");
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FixtureStatEvents);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishFixtureEventsToGuild(guildId, channelId, message.FixtureEvents), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
        }
    }

    private static readonly FplEvent[] FixtureStatEvents =
    [
        FplEvent.FixtureGoals,
        FplEvent.FixtureAssists,
        FplEvent.FixtureCards,
        FplEvent.FixturePenaltyMisses
    ];
```

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `FixtureEventE2ETests` and `DiscordFixtureEventsHandlerTests` pass unchanged. If `DiscordFixtureEventsHandlerTests` fails, read the failing assertion carefully — it may have been (incorrectly) relying on a message being published to an unsubscribed channel; if so, stop and flag it rather than forcing the old behavior back.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackFixtureEventsHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordFixtureEventsHandler.cs
git commit -m "Use fast subscription query in fixture-events handlers"
```

---

## Task 10: Migrate gameweek-finished handlers (Slack + Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackGameweekFinishedHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordGameweekFinishedHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/GameweekEventPublishingE2ETests.cs` (`OnGameweekFinished_PublishesGameweekFinished`), `src/FplBot.Tests/E2E/Discord/DiscordGameweekFinishedHandlerTests.cs`

**Interfaces:**
- Consumes: `GetChannelSubscription(installationId, channelId)` from Task 1, to fetch `FollowedLeagueId` for each Standings-subscribed channel.

**Behavior note (Discord only):** today `Consume<GameweekFinished>` publishes `PublishGameweekFinishedToGuild` to **every channel of every installation** regardless of subscription; the downstream consumer immediately discards anything not `IsSubscribedTo(Standings)`. After this change only Standings-subscribed channels are published to — same eventual Discord messages, far fewer bus messages and `FindInstallationByTeamId` calls.

- [ ] **Step 1: Rewrite `SlackGameweekFinishedHandler.Consume<GameweekFinished>`**

Replace lines 20-32 of `SlackGameweekFinishedHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<GameweekFinished> context)
    {
        var notification = context.Message;
        var subscribedChannels = await teamsRepo.GetChannelsSubscribedTo(FplEvent.Standings);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            var channel = await teamsRepo.GetChannelSubscription(teamId, channelId);
            if (channel is not null && channel.FollowedLeagueId is not null)
            {
                await context.Publish(new PublishStandingsToSlackWorkspace(teamId, channelId, (int)channel.FollowedLeagueId.Value, notification.FinishedGameweek.Id));
            }
        }
    }
```

- [ ] **Step 2: Rewrite `DiscordGameweekFinishedHandler.Consume<GameweekFinished>`**

Replace lines 20-33 of `DiscordGameweekFinishedHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<GameweekFinished> context)
    {
        var message = context.Message;
        logger.LogInformation($"Gameweek {message.FinishedGameweek.Id} finished");
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.Standings);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            var channel = await repo.GetChannelSubscription(guildId, channelId);
            var leagueId = channel?.FollowedLeagueId is { } id ? (int)id.Value : (int?)null;
            await context.Publish(new PublishGameweekFinishedToGuild(guildId, channelId, leagueId, message.FinishedGameweek.Id));
        }
    }
```

Leave `Consume<PublishGameweekFinishedToGuild>` (lines 35-45) untouched — its `sub.IsSubscribedTo(FplEvent.Standings)` guard is now always true but harmless to keep as a defensive check.

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `GameweekEventPublishingE2ETests.OnGameweekFinished_PublishesGameweekFinished` and `DiscordGameweekFinishedHandlerTests` pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackGameweekFinishedHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordGameweekFinishedHandler.cs
git commit -m "Use fast subscription query in gameweek-finished handlers"
```

---

## Task 11: Migrate gameweek-started handlers (Slack + Discord)

**Files:**
- Modify: `src/FplBot/Services/EventHandlers/Slack/SlackGameweekStartedHandler.cs`
- Modify: `src/FplBot/Services/EventHandlers/Discord/DiscordGameweekStartedHandler.cs`
- Test: `src/FplBot.Tests/E2E/Slack/SlackSubscriptions/GameweekEventPublishingE2ETests.cs` (`OnGameweekTransition_PublishesGameweekJustBegan`, `FromPreseason_ToGw1_PublishesGw1Start`), `src/FplBot.Tests/E2E/Discord/DiscordGameweekStartedHandlerTests.cs`

**Behavior note:** today both first-stage consumers fan out `ProcessGameweekStartedFor*` to every installation (Slack) / every channel of every installation (Discord) unconditionally; the second-stage handler's per-channel logic only ever produces messages for channels subscribed to `Captains` or `Transfers` (everything else results in an empty `messages` list, a no-op publish). After this change, only channels subscribed to `Captains` or `Transfers` are fanned out to at all — same eventual messages, far less wasted second-stage work.

- [ ] **Step 1: Rewrite `SlackGameweekStartedHandler.Consume<GameweekJustBegan>`**

Replace lines 26-34 of `SlackGameweekStartedHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var notification = context.Message;
        var subscribedChannels = await teamsRepo.GetChannelsSubscribedTo(FplEvent.Captains, FplEvent.Transfers);
        var subscribedInstallationIds = subscribedChannels.Select(c => c.InstallationId).Distinct();
        foreach (var installationId in subscribedInstallationIds)
        {
            await context.Publish(new ProcessGameweekStartedForSlackWorkspace(installationId, notification.NewGameweek.Id));
        }
    }
```

Leave `Consume<ProcessGameweekStartedForSlackWorkspace>` and `DoSubHandling` (lines 36-123) untouched — they operate on one already-known installation and its full channel list, which is correct as-is.

- [ ] **Step 2: Rewrite `DiscordGameweekStartedHandler.Consume<GameweekJustBegan>`**

Replace lines 23-34 of `DiscordGameweekStartedHandler.cs`:

```csharp
    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var notification = context.Message;
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.Captains, FplEvent.Transfers);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new ProcessGameweekStartedForGuildChannel(guildId, channelId, notification.NewGameweek.Id));
        }
    }
```

Leave `Consume<ProcessGameweekStartedForGuildChannel>` (lines 36-131) untouched.

- [ ] **Step 3: Run the targeted tests**

Run: `dotnet run --project src/Build -- test`
Expected: `GameweekEventPublishingE2ETests` (`OnGameweekTransition_PublishesGameweekJustBegan`, `FromPreseason_ToGw1_PublishesGw1Start`) and `DiscordGameweekStartedHandlerTests` pass unchanged.

- [ ] **Step 4: Commit**

```bash
git add src/FplBot/Services/EventHandlers/Slack/SlackGameweekStartedHandler.cs src/FplBot/Services/EventHandlers/Discord/DiscordGameweekStartedHandler.cs
git commit -m "Use fast subscription query in gameweek-started handlers"
```

---

## Task 12: Full-suite verification and cleanup pass

**Files:** none (verification only)

- [ ] **Step 1: Run the entire test suite once, end to end**

Run: `dotnet run --project src/Build -- test`
Expected: all tests green, zero build warnings.

- [ ] **Step 2: Grep for any remaining `GetAllInstallations()` call sites that filter by a single event or small event set**

Run: `grep -rn "GetAllInstallations" src/FplBot/Services/EventHandlers/`
Expected remaining call sites are only ones that genuinely need every installation regardless of subscription (there should be none left among Slack/Discord event handlers after Tasks 2-11 — if any turn up, they were missed and need a follow-up task, not silent skipping).

- [ ] **Step 3: Confirm `Installation.GetSubscriptionsTo` has no remaining callers in `Services/EventHandlers/`**

Run: `grep -rn "GetSubscriptionsTo" src/FplBot/Services/EventHandlers/`
Expected: no results (every call site was migrated in Tasks 2-11).
