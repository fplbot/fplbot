# Contributing

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (for local infrastructure via Aspire AppHost)

## Architecture overview

FplBot runs as a single .NET project (`src/FplBot/`) deployed as four Docker containers, each handling a different role:

| Service | Role |
|---|---|
| `WebApi` | HTTP: Slack/Discord webhooks, OAuth, slash commands |
| `EventHandlers` | Consumes events from the bus → posts messages to Slack/Discord |
| `EventPublishers` | Background jobs polling FPL API and publishing events |
| `SearchIndexer` | Syncs FPL data to Elasticsearch for search |

Events flow through MassTransit with Azure Service Bus. See `CLAUDE.md` for the full architecture and patterns.

## Local development

Start the local infrastructure (Redis + service bus emulator) via the Aspire AppHost:

```shell
./src/devenv.sh
```

Then run the app:

```shell
dotnet run --project src/FplBot
```

**Dev credentials are included.** `src/FplBot/appsettings.json` ships with working credentials for a private dev Slack workspace and Discord server — clone and run, no setup required.

When using the Aspire AppHost, `REDIS_URL` and `ASB_CONNECTIONSTRING` are injected automatically.

To see the bot in action, join the dev environments:
- **Slack**: [FplBot Dev workspace](#) _(ask a maintainer for the invite link)_
- **Discord**: [FplBot Dev server](#) _(ask a maintainer for the invite link)_

If you want to test against your own Slack/Discord apps instead, add an `appsettings.Local.json` (gitignored) in `src/FplBot/` with the values below, or set equivalent environment variables.

### Configuration reference

| Key | Description |
|---|---|
| `REDIS_URL` | Redis connection string |
| `ASB_CONNECTIONSTRING` | Azure Service Bus connection |
| `CLIENT_ID` | Slack app client ID |
| `CLIENT_SECRET` | Slack app client secret |
| `CLIENT_SIGNING_SECRET` | Slack signing secret (for request verification) |
| `SlackAppId` | Slack application ID |
| `SlackToken_FplBot_Workspace` | Bot token for internal workspace |
| `DISCORD_CLIENT_ID` | Discord app client ID |
| `DISCORD_CLIENT_SECRET` | Discord app client secret |
| `DISCORD_PUBLICKEY` | Discord app public key (interaction verification) |
| `DISCORD_TOKEN` | Discord bot token |
| `DiscordAppId` | Discord application ID |
| `fpl.Login` | FPL API login email |
| `fpl.Password` | FPL API password |

### Rotating dev credentials

If a token is compromised: regenerate it in the Slack/Discord dashboard, update `src/FplBot/appsettings.json`, and commit. Takes ~5 minutes.

## Running tests

```shell
dotnet run --project src/Build -- test
```

Or directly:

```shell
dotnet test src
```

## Adding a feature

For the two most common tasks, use the Claude skill files in `src/.claude/commands/`:

- **New notification type** (e.g. alert on a new FPL event): `add-notification.md`
- **New chat command** (e.g. `@fplbot <something>`): `add-command-handler.md`

Or see `CLAUDE.md` at the repo root for the full architecture reference.

## Building and deploying

The build system lives in `src/Build/Program.cs` (Bullseye targets):

```shell
# Build Docker images
dotnet run --project src/Build -- docker-build

# Push to test registry and release
dotnet run --project src/Build -- docker-push-test deploy-test

# Push to prod registry and release
dotnet run --project src/Build -- docker-push-prod deploy-prod
```

Requires `HEROKU_TOKEN` for push and `HEROKU_API_KEY` for release.

## Environments

### Dev (local)
- Slack bot app: https://api.slack.com/apps/A0BV9MKL214/ — credentials at root of `appsettings.json`
- Slack admin login app: https://api.slack.com/apps/A0BUW9JBL0P/ — credentials under `admin:{}` in `appsettings.json`
- Manifests + recreation: `src/slack-app-manifest.json` / `src/slack-admin-app-manifest.json`, run `python3 src/create-slack-dev-app.py [--admin]`
- Event subscriptions not configured — requires ngrok to expose `localhost:1337` first

### Test
- Heroku: https://dashboard.heroku.com/apps/blank-fplbot-test/
- Slack app: https://api.slack.com/apps/ATDD4SFQ9/
- Discord app: https://discord.com/developers/applications/812441913193529365/information

### Production
- Heroku: https://dashboard.heroku.com/apps/blank-fplbot/
- Slack app: https://api.slack.com/apps/AREFP62B1
- Discord app: https://discord.com/developers/applications/812441954175811664/information
