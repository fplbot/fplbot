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

Then run the app. With no `--services` flag it runs all four services in one process:

```shell
dotnet run --project src/FplBot
```

Pass the flag to narrow it down — `--services WebApi`, `--services WebApi,EventHandlers`, or
`--services All` — which is how the deployed containers each run a single role.

**No credentials needed.** `src/FplBot/appsettings.json` ships with safe placeholders, and in the
`Development` environment every outbound Slack/Discord call is faked (see below). Clone and run.

When using the Aspire AppHost, `REDIS_URL` and `ASB_CONNECTIONSTRING` are injected automatically.
The AppHost also seeds fake Slack workspaces and Discord guilds into Redis on startup
(`src/FplBot.AppHost/DevSeederLifecycleHook.cs`), so there's data to work against immediately.

### Launch profiles (`dotnet run`)

`src/FplBot/Properties/launchSettings.json` holds two profiles. `dotnet run` picks the first unless
you name another, which is why the bare command above lands in `Development`:

| Profile | `DOTNET_ENVIRONMENT` | Outbound Slack/Discord |
|---|---|---|
| `Default` | `Development` | faked — logged, never sent |
| `Integration` | `Integration` | live |

```shell
dotnet run --project src/FplBot                                       # Development
dotnet run --project src/FplBot --launch-profile Integration          # Integration
dotnet run --project src/FplBot --launch-profile Integration -- --services WebApi
```

`dotnet watch --project src/FplBot` works too, hot reload included — handy when iterating on
handlers, since the recurring jobs keep ticking through the reloaded code.

`--launch-profile` belongs to `dotnet run` and goes before `--`; everything after `--` is passed to
the app. Setting `DOTNET_ENVIRONMENT` yourself overrides the profile.

### Frontend (ClientApp)

The admin UI and public pages are a Vue 3 + Vite app in
`src/FplBot/Services/WebApi/ClientApp`. **You need one of the two options below** — the built SPA is
not in the repo (`wwwroot/.gitignore` excludes `/index.html` and `/assets`), so on a fresh clone the
backend has nothing to serve at `/`.

**Option 1 — Vite dev server** (what you want while changing frontend code; gives HMR):

```shell
./src/run.sh          # backend + Vite together, stops both on Ctrl-C
```

or the two halves separately, if you want them in their own terminals:

```shell
cd src/FplBot/Services/WebApi/ClientApp
npm ci
npm run dev
```

Then browse **http://localhost:5173**, not the backend port. Vite proxies everything to the backend
on `https://localhost:1337`, so the app must be running too. This is also the flow the OAuth
redirects assume locally — on success they send you to `http://localhost:5173`.

**Option 2 — build once into `wwwroot`** (fine if you're only touching backend code):

```shell
dotnet run --project src/Build -- client-build
```

That runs `npm ci` + `npm run build`, emitting `index.html` and `assets/` into
`src/FplBot/Services/WebApi/wwwroot`, which the backend serves as static files. Then browse the
backend directly at **https://localhost:1337**. Re-run it after pulling frontend changes.

#### How local differs from production

In production there is no Vite and no Node — the runtime image is just
`mcr.microsoft.com/dotnet/aspnet` with the published backend copied in. The SPA is built at
image-build time (`BuildImage` in `src/Build/Program.cs` runs `client-build` before
`docker build`), so `index.html` and `assets/` ship inside the image and the WebApi container
serves them as static files from the same origin as the API.

| | builds the SPA | serves it | origin |
|---|---|---|---|
| local, frontend work | `npm run dev` (in memory, never written to disk) | Vite on `:5173`, proxying to `:1337` | two origins |
| local, backend work | `client-build` → `wwwroot` | the backend on `:1337` | one origin |
| production | `docker-build` → `wwwroot` → image | the WebApi container | one origin |

That two-origin split is the only real deviation, and the backend compensates for it explicitly:

```csharp
var successUri = env.IsLocal() ? "http://localhost:5173/success" : "/success";
```

OAuth has to redirect to Vite's origin locally, but can use a relative path in production. If you
hit an OAuth flow that lands on the wrong port locally, that branch is why. The Vite proxy
(`vite.config.ts`) exists for the same reason and has no production counterpart.

#### API types are hand-maintained

There is no code generation — no OpenAPI/Swagger document, no NSwag, no `codegen` script. The
TypeScript types in `ClientApp/src/api/types.ts` are written by hand to mirror the JSON shapes the
`/api/**` endpoints accept and return, so **changing a backend DTO means editing that file too**.

`client-build` runs `npm run typecheck` (`vue-tsc --noEmit`) between `npm ci` and `npm run build`,
so a drifted type fails the build — locally and in CI, since the `ci` target is `test` +
`client-build`. `vite build` itself never type-checks; the explicit step is what catches this.

TypeScript is pinned to 6.x on purpose. `vue-tsc` resolves `typescript/lib/tsc.js`, which
TypeScript 7 (the native rewrite) no longer exposes through its package `exports` — on 7.x the
typecheck dies with `ERR_PACKAGE_PATH_NOT_EXPORTED`. Don't take a Renovate bump to `typescript@7`
until `vue-tsc` supports it.

### Rider run configurations

They live in `src/.idea/.idea.FplBot/.idea/runConfigurations/` and do not use the launch profiles —
each sets `--services <Service>` and `DOTNET_ENVIRONMENT` directly, and every service has an
`(Integration)` twin:

| Configuration | Runs | Environment |
|---|---|---|
| `WebApi` / `EventHandlers` / `EventPublishers` / `SearchIndexer` | that one service | `Development` |
| `WebApi (Integration)` / … | that one service | `Integration` |
| `All Services` | all four + `SPA (Vite dev)` | `Development` |
| `All Services (Integration)` | all four + `SPA (Vite dev)` | `Integration` |
| `WebApi + Vite` | WebApi + `SPA (Vite dev)` | `Development` |
| `SPA (Vite dev)` | `npm run dev` in `ClientApp` (see Frontend above) | — |

Outside Rider, the equivalent is the `Integration` profile above.

### Development is mock-based

In `Development`, `DevLoggingSlackClient` and `DevLoggingDiscordClient` stand in for the real
clients: writes become log lines with a canned success response, reads return static fake data
(fake installed slash commands, the seeded channel `C0DEV000001` in Slack's channel list) so the
admin UI works end to end against the Redis seed.

This happens **regardless of whether real credentials are configured** — setting user secrets does
not make `Development` talk to the real APIs. Use `Integration` for that.

Consequences worth knowing:

- A deferred Discord slash command never gets its followup, so it sits on "thinking…" forever.
- Slack request signature verification on `/events` is **off** in `Development`, on in `Integration`.
- Discord interaction signature verification is **on everywhere**; in `Development` only, it can be
  disabled with `SKIP_DISCORD_SIGNATURE_VERIFICATION=true`.

In code: `env.IsLocal()` covers both `Development` and `Integration` and guards machine
conveniences (https on localhost, Aspire telemetry, `[MachineName]` message prefix). Plain
`env.IsDevelopment()` is reserved for the branches that fake an outbound integration.

### Integration: testing against real Slack/Discord

`Integration` is local in every other respect — same `appsettings.json`, same user secrets, https
on localhost, telemetry to the Aspire dashboard — but every integration is live. There is no
`appsettings.Integration.json`, so any key you don't override stays at its placeholder value.

Supply real credentials with [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets),
never `appsettings.json` — see [Credentials](#credentials).

#### Somewhere to test

Don't point a development app at a workspace or server people actually use — a half-finished
notification lands in front of them, and every restart re-posts. There's a throwaway of each,
open to anyone contributing:

- Slack: https://join.slack.com/t/fplbotdevthro-spe4676/shared_invite/zt-4aryvvlls-09QreAxPL_Nc9t7IlJrXTg
- Discord: https://discord.gg/kmUnuTVgQ

Use the existing dev apps against them — `@fplbotdevelop` on Slack and the dev Discord
application, both linked under [Dev (local)](#dev-local) — rather than registering your own.
Ask a maintainer for their credentials and put them in user secrets as below.

Always install from the **Install fplbot** buttons on the app's own front page — the Vue app on
https://localhost:5173 — or from the admin pages. Never from an install link in Slack's or
Discord's own dashboards: only our own flow completes the install and stores the token.

#### Slack

```shell
dotnet user-secrets set CLIENT_ID "..." --project src/FplBot
dotnet user-secrets set CLIENT_SECRET "..." --project src/FplBot
dotnet user-secrets set CLIENT_SIGNING_SECRET "..." --project src/FplBot
dotnet user-secrets set SlackAppId "..." --project src/FplBot
```

#### Discord

```shell
dotnet user-secrets set DISCORD_TOKEN "..." --project src/FplBot
dotnet user-secrets set DISCORD_CLIENT_ID "..." --project src/FplBot
dotnet user-secrets set DISCORD_CLIENT_SECRET "..." --project src/FplBot
dotnet user-secrets set DISCORD_PUBLICKEY "..." --project src/FplBot
dotnet user-secrets set DiscordAppId "..." --project src/FplBot
```

All five Discord values must come from the *same* Discord Application (Developer Portal → your app
→ Bot tab for the token, General Information for the rest). Mixing values from different apps fails
in confusing ways: wrong bot invited, signature verification failures. If you ever do need an Application of your own,
creating one is a manual step at discord.com/developers/applications — there's no API for it.

With the secrets in place, run against them:

```shell
dotnet run --project src/FplBot --launch-profile Integration
```

Real Slack and Discord sign their webhooks, so to receive events you also need a public URL
(ngrok) pointed at `https://localhost:1337` and registered in the app's dashboard.

`src/FplBot` and `src/FplBot.AppHost` share the `fplbot-secrets` user-secrets store, so both
`--project` targets write to the same file. A real bot token for a *seeded* dev workspace goes in
the AppHost's view of it:

```shell
dotnet user-secrets set DEV_SEED_SLACK_TOKEN "..." --project src/FplBot.AppHost
```

Note that the AppHost runs as `Production`; it picks up user secrets only because of the explicit
`AddUserSecrets("fplbot-secrets")` call in its `Program.cs`.

### Credentials

Never put a real token in `appsettings.json`, not even for a throwaway app. GitHub's secret
scanning partnership with Slack and Discord detects and revokes committed tokens the moment they're
pushed, regardless of intent. User secrets live outside the repo in your user profile and are loaded
in both `Development` and `Integration`, across all four services, with no code changes.

If a placeholder in `appsettings.json` ever needs changing, that's a config change like any other —
but the value itself must stay a placeholder.

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
| `SKIP_DISCORD_SIGNATURE_VERIFICATION` | `Development` only: accept unsigned Discord interactions |

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
- Slack bot app (`@fplbotdevelop`): https://api.slack.com/apps/A0BV9MKL214/ — credentials at root of `appsettings.json`
- Slack admin login app (`Admin Portal [dev]` — Sign in with Slack for the local admin pages): https://api.slack.com/apps/A0BUW9JBL0P/ — credentials under `admin:{}` in `appsettings.json`
- Discord app: https://discord.com/developers/applications/895012635144224830/information
- Manifests + recreation: `src/slack-app-manifest.json` / `src/slack-admin-app-manifest.json`, run `python3 src/create-slack-dev-app.py [--admin]`
- Event subscriptions not configured — requires ngrok to expose `localhost:1337` first
- Throwaway workspace and server to install into: see [Integration](#integration-testing-against-real-slackdiscord)

### Test
- Heroku: https://dashboard.heroku.com/apps/blank-fplbot-test/
- Slack app: https://api.slack.com/apps/ATDD4SFQ9/
- Discord app: https://discord.com/developers/applications/812441913193529365/information

### Production
- Heroku: https://dashboard.heroku.com/apps/blank-fplbot/
- Slack app: https://api.slack.com/apps/AREFP62B1
- Discord app: https://discord.com/developers/applications/812441954175811664/information
