# Contributing

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (for local infrastructure via Aspire AppHost)

## Local development

Start the local infrastructure (Redis + service bus emulator) via the Aspire AppHost:

```shell
./src/devenv.sh
```

Then add an `appsettings.Local.json` file (gitignored) in the relevant service directory with the config below, or set equivalent environment variables.

### Configuration

| Key | Description |
|-----|-------------|
| `fpl__login` | FPL account email |
| `fpl__password` | FPL account password |
| `ConnectionStrings__redis` | Redis connection string (or `REDIS_URL`) |
| `ConnectionStrings__servicebus` | ASB connection string (or `ASB_CONNECTIONSTRING`) |
| `CLIENT_ID` | Slack app client ID |
| `CLIENT_SECRET` | Slack app client secret |
| `DISCORD_CLIENT_ID` | Discord app client ID |
| `DISCORD_CLIENT_SECRET` | Discord app client secret |
| `DISCORD_PUBLICKEY` | Discord app public key |
| `DISCORD_TOKEN` | Discord bot token |
| `DiscordAppId` | Discord application ID |

When using the Aspire AppHost, `ConnectionStrings__redis` and `ConnectionStrings__servicebus` are injected automatically.

## Running tests

```shell
dotnet run --project src/Build -- test
```

Or directly:

```shell
dotnet test src
```

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
