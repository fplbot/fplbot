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

**No secrets are needed to run locally.** All keys have safe development defaults in `src/FplBot/appsettings.json`. For integration with real Slack/Discord apps, add an `appsettings.Local.json` (gitignored) in `src/FplBot/` with the values below, or set equivalent environment variables.

When using the Aspire AppHost, `REDIS_URL` and `ASB_CONNECTIONSTRING` are injected automatically.

### Configuration reference

| Key | Description | Dev default |
|---|---|---|
| `REDIS_URL` | Redis connection string | `rediss://:devpassword@localhost:6379` (Aspire) |
| `ASB_CONNECTIONSTRING` | Azure Service Bus connection | Local emulator (Aspire) |
| `CLIENT_ID` | Slack app client ID | `dev` |
| `CLIENT_SECRET` | Slack app client secret | `dev` |
| `CLIENT_SIGNING_SECRET` | Slack signing secret (for request verification) | `dev` |
| `SlackAppId` | Slack application ID | `dev` |
| `SlackToken_FplBot_Workspace` | Bot token for internal workspace | `dev` |
| `DISCORD_CLIENT_ID` | Discord app client ID | `dev` |
| `DISCORD_CLIENT_SECRET` | Discord app client secret | `dev` |
| `DISCORD_PUBLICKEY` | Discord app public key (interaction verification) | `dev` |
| `DISCORD_TOKEN` | Discord bot token | `dev` |
| `DiscordAppId` | Discord application ID | `dev` |
| `fpl.Login` | FPL API login email | `dev` |
| `fpl.Password` | FPL API password | `dev` |

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

### Test
- Heroku: https://dashboard.heroku.com/apps/blank-fplbot-test/
- Slack app: https://api.slack.com/apps/ATDD4SFQ9/
- Discord app: https://discord.com/developers/applications/812441913193529365/information

### Production
- Heroku: https://dashboard.heroku.com/apps/blank-fplbot/
- Slack app: https://api.slack.com/apps/AREFP62B1
- Discord app: https://discord.com/developers/applications/812441954175811664/information
