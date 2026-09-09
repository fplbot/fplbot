---
name: verify
description: How to runtime-verify a change in the fplbot monolith (WebApi + EventHandlers, MassTransit, Redis)
---

# Verifying a change in fplbot

## Local infra

Usually already running via the user's own `FplBot.AppHost` (Aspire) — check before starting
anything yourself (never launch `devenv.sh` or `FplBot.AppHost`):

```bash
docker ps --format '{{.Names}}\t{{.Status}}'   # look for redis-*, elasticsearch-*
ps aux | grep -i "FplBot --services\|AlmostServiceBus"
```

If Redis/ServiceBus/Elasticsearch containers are up but the `FplBot --services ...` processes
predate your code changes, ask the user before touching them — restarting only the specific
services you need (e.g. WebApi + EventHandlers) is usually fine; leave AppHost, Redis, ServiceBus,
and any services unrelated to your change alone.

## Building + running a service standalone

```bash
dotnet build --no-incremental src/FplBot/FplBot.csproj
cd src/FplBot
ASPNETCORE_ENVIRONMENT=Development ./bin/Debug/net10.0/FplBot --services WebApi > /tmp/webapi.log 2>&1 &
DOTNET_ENVIRONMENT=Development   ./bin/Debug/net10.0/FplBot --services EventHandlers > /tmp/eventhandlers.log 2>&1 &
```

**Gotcha:** WebApi runs as a `WebApplication` (reads `ASPNETCORE_ENVIRONMENT`). EventHandlers/
EventPublishers/SearchIndexer run as a generic `Host` via `Host.CreateDefaultBuilder`
(`FplBotApplication.RunAsWorkerHost`), which reads `DOTNET_ENVIRONMENT` instead. Set both env vars
when you need Development behavior across services, or check the "Hosting environment: ..." line
in the startup log to confirm which one landed.

There is no `appsettings.Development.json` — dev safety comes from `env.IsDevelopment()` checks in
code (dev-logging Slack/Discord client wrappers), not config overrides. Default `ASPNETCORE_ENVIRONMENT`/
`DOTNET_ENVIRONMENT` when unset is `Production`.

## Redis access

`redis-cli` isn't installed on the host; exec into the running container (name varies per Aspire
run, check `docker ps`):

```bash
docker exec <redis-container-name> redis-cli --tls --insecure -a devpassword <command>
```

Key convention: `TeamId-{slackTeamId}` (hash with `accessToken`, `fplchannel`, `fplleagueId`,
`teamName`, `subscriptions` fields). Seed a throwaway team to test a specific path, then delete it:

```bash
docker exec <redis> redis-cli --tls --insecure -a devpassword HSET "TeamId-VERIFY-SLACK" \
  accessToken "xoxb-fake" fplchannel "C0VERIFY001" fplleagueId "12345" \
  teamName "Verify" subscriptions "FixtureFullTime"
# ... test ...
docker exec <redis> redis-cli --tls --insecure -a devpassword DEL "TeamId-VERIFY-SLACK"
```

## Debug endpoints

`Services/WebApi/Endpoints/Test/DebugRoute.cs` maps test-only GET endpoints under `/debug` (not
`/test` — check `WebAppExtensions.cs` for the base route), e.g. `/debug/fixturefinished?fixtureId=1`.
These publish real MassTransit events onto the bus for EventHandlers to consume — this is the
fastest way to runtime-verify a consumer end-to-end without waiting on the real FPL API poll cycle.
Blocked in Production (`env.IsProduction()` check in each handler).

```bash
curl -sk -w "\nHTTP %{http_code}\n" "https://localhost:1337/debug/fixturefinished?fixtureId=1"
```

## Reading results

Both WebApi and EventHandlers log to stdout (Serilog console sink) — `tail -f /tmp/*.log`. In
Development, Slack/Discord posts are short-circuited by `DevLoggingSlackClient/DevLoggingDiscordClient`
and show up as `[DEV] Slack → {channel}` / `[DEV] Discord → channel:{id}` info-level log lines
instead of hitting the real APIs — that's your signal the flow completed without needing real
tokens.

Fixture data (`IFixtureClient`, `IGlobalSettingsClient`, `ILiveClient`) hits the real
fantasy.premierleague.com API even locally — no fake/mock in the running app, only in unit tests.
Fixture id 1 reliably exists; a large bogus id (e.g. `999999`) is a quick way to exercise the
"fixture not found" branch of a handler.
