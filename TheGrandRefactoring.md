## Task Progress

- [x] Step 1 — Create FplBot project skeleton (`src/FplBot/FplBot.csproj`, `Program.cs`, `--services` parsing, added to sln, `Directory.Packages.props` bumped to .NET 10 + MassTransit 8.3.6)
- [x] Step 2 — Migrate and convert projects (bottom-up, NServiceBus → MassTransit) — 379 files, all 13 library/host projects folded in
- [x] Step 3 — Create service modules
- [x] Step 4 — Remove Azure Functions and Pulumi
- [x] Step 5 — Multi-stage Dockerfile
- [x] Step 6 — Update CI workflows
- [x] Step 8 — Aspire AppHost for local dev
- [ ] Step 9 — CI integration test infrastructure
- [ ] Step 10 — Update test projects
- [ ] Step 11 — Delete old projects

---

# Goals

* Simplify the code base
* Simplify builds (CI, images)
* Reduce build times (images, dotnet build)
* Bump to .NET 10 throughout

I want to create a new structure for the fplbot project.

I want to end up with a _single_ FplBot.csproj, with everything just organised in folders instead of spread over csprojs.

## Everything goes in

All projects — both host projects and library projects — are folded into the single FplBot.csproj. Nothing is published as a NuGet package. These are all internal libraries with no external consumers:

- `Fpl.Client` → internal HTTP client for the FPL API
- `Fpl.Search` → internal search logic
- `Fpl.EventPublishers` → internal event publishing
- `FplBot.Data` → internal persistence
- `FplBot.Formatting` → internal formatting helpers
- `FplBot.Messaging.Contracts` → internal message contracts
- `FplBot.VerifiedEntries` → internal verified entries logic
- `FplBot.EventHandlers.Slack` / `FplBot.EventHandlers.Discord` → internal event handlers
- `FplBot.WebApi.Slack` / `FplBot.WebApi.Discord` → internal web api modules
- etc.

The `ext/` projects (`Discord.Net.Endpoints`, `Discord.Net.HttpClients`) are also folded in.

`FplBot.Infrastructure` (Pulumi) and `FplBot.Functions` (Azure Functions) are **deleted entirely** — not folded in. The Azure Functions trigger is entirely commented out (dead code), so there is nothing to preserve.

## Services

Today's host projects:

1. `FplBot.WebApi`
2. `FplBot.EventHandlers.Console`
3. `Fpl.EventPublishers.Console`
4. `Fpl.Search.Indexer.Console`

...become named services passed as an argument when running the single FplBot host project:

```
dotnet run --project FplBot.csproj --services "WebApi"
dotnet run --project FplBot.csproj --services "WebApi,EventHandlers"
dotnet run --project FplBot.csproj --services "EventPublishers,EventHandlers"
dotnet run --project FplBot.csproj --services "All"
```

Each service is its own `IHostedService`. There's an interface per service that configures DI and any host-specific details (e.g. the WebApi service sets up the request pipeline).

## Docker

One Dockerfile with multiple named final stages — one per service. Each stage bakes in its own CMD. Any container platform targets the right stage; no platform-specific config files needed.

```
docker build --target web       -t fplbot-web .
docker build --target eventhandler -t fplbot-eventhandler .
```

BuildKit shares the build and base stages across all targets — effectively one compile, 4 images.

## Folder structure

Instead of separate csprojs, everything is folders inside FplBot.csproj:

```
FplBot.csproj
 ├── Data/               (was FplBot.Data.csproj)
 ├── Fpl/
 │    ├── Client/        (was Fpl.Client.csproj)
 │    └── Search/        (was Fpl.Search.csproj)
 ├── Formatting/         (was FplBot.Formatting.csproj)
 ├── Messaging/          (was FplBot.Messaging.Contracts.csproj)
 └── Services/
      ├── EventHandlers/ (was FplBot.EventHandlers.*)
      ├── EventPublishers/ (was Fpl.EventPublishers.*)
      ├── Search/        (was Fpl.Search.Indexer.Console)
      └── WebApi/        (was FplBot.WebApi.*)
```

Tests stay in their own test projects (`FplBot.Tests`, `FplBot.WebApi.Tests`) — test projects referencing a single main csproj is clean and standard.


=== PART 2 ====

I want it to be easier for anyone to get into the code base (beginners, contributors).

Use a .NET Aspire AppHost (`src/FplBot.AppHost`) to spin up local dependencies — see Step 8 in the Execution Plan. Running `dotnet run --project src/FplBot.AppHost` starts Redis and an Azure Service Bus emulator (AlmostServiceBus) and launches the services wired to them automatically.

---

## Execution Plan

### Commit discipline

Commit and push after each incremental step. Every commit must leave the codebase in a workable state (builds). Tests may be broken during the migration — that's acceptable. Commit messages should clearly describe what moved/changed. Push to `jk/simplifiy-builds` (current branch) after each step.

### Step 1 — Create the new FplBot project skeleton

- Create `src/FplBot/FplBot.csproj` (`Microsoft.NET.Sdk.Web`, `net10.0`)
- Update `src/Directory.Packages.props`: bump `AspNetVersion` to `10.0.0`, add `MassTransit` + `MassTransit.Azure.ServiceBus.Core` pinned to `8.*` (v8 is Apache 2.0; v9+ requires a commercial license), remove all `NServiceBus.*` entries
- Collect all remaining `<PackageReference>` from every project being folded in and add to the new csproj
- Create `src/FplBot/Program.cs` with `--services` argument parsing skeleton
- Add to `src/FplBot.sln`

`--services` values: `WebApi`, `EventHandlers`, `EventPublishers`, `SearchIndexer`, `All`

### Step 2 — Migrate and convert projects (bottom-up, one pass)

Move files into new folders and convert NServiceBus → MassTransit as you go. After each project: `dotnet build src/FplBot`, then remove old `.csproj` from solution.

**Conversion rules applied during migration:**

| NServiceBus | MassTransit |
|---|---|
| `IHandleMessages<T>` | `IConsumer<T>` |
| `IMessage`/`IEvent`/`ICommand` marker | Remove — plain POCOs |
| `IBus.Publish(...)` | `IPublishEndpoint.Publish(...)` |
| `IBus.Send(...)` | `ISendEndpointProvider.Send(...)` |
| `NSB_LICENSE` env var | Gone — no license needed |

Migration order:

1. `FplBot.Messaging.Contracts` → `src/FplBot/Messaging/` — strip any NServiceBus marker interfaces, keep as plain POCOs
2. `Fpl.Client` → `src/FplBot/Fpl/Client/`
3. `FplBot.Data` → `src/FplBot/Data/`
4. `ext/Discord.Net.Endpoints` + `ext/Discord.Net.HttpClients` → `src/FplBot/ext/`
5. `FplBot.VerifiedEntries` → `src/FplBot/VerifiedEntries/`
6. `Fpl.EventPublishers` → `src/FplBot/Services/EventPublishers/` — replace `IBus.Publish` with `IPublishEndpoint.Publish`
7. `Fpl.Search` → `src/FplBot/Fpl/Search/`
8. `FplBot.Formatting` → `src/FplBot/Formatting/`
9. `FplBot.EventHandlers.Discord` → `src/FplBot/Services/EventHandlers/Discord/` — `IHandleMessages<T>` → `IConsumer<T>`
10. `FplBot.EventHandlers.Slack` → `src/FplBot/Services/EventHandlers/Slack/` — same
11. `FplBot.WebApi.Discord` → `src/FplBot/Services/WebApi/Discord/`
12. `FplBot.WebApi.Slack` → `src/FplBot/Services/WebApi/Slack/`
13. `FplBot.WebApi` → `src/FplBot/Services/WebApi/`

Key files to carry over (reuse, don't rewrite):
- `src/Fpl.Client/Infra/IFplApiClientServiceCollectionExtensions.cs` — `AddFplApiClient()`
- `src/Fpl.EventPublishers/FplWorkerServiceCollectionExtensions.cs` — `AddFplWorkers()`
- `src/Fpl.Search/SearchServiceCollectionExtensions.cs` — `AddRecurringIndexer()`
- `src/FplBot.VerifiedEntries/ServiceCollectionExtensions.cs` — `AddVerifiedEntries()`
- `src/FplBot.WebApi/Infrastructure/WebApplicationBuilderExtensions.cs` — `ConfigureWebApp()` / `UseWebApp()`

### Step 3 — Create service modules

```csharp
public interface IFplBotServiceModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration config);
}

public interface IWebServiceModule : IFplBotServiceModule
{
    void ConfigureApp(WebApplication app);
}
```

Four modules. MassTransit creates a separate ASB queue per consumer type automatically (`cfg.ConfigureEndpoints(ctx)`), so Discord and Slack consumers never share a queue — no isolation tricks needed:

- `WebApiModule` — `ConfigureWebApp()` + `UseWebApp()`
- `EventHandlersModule` — `AddMassTransit` with all Discord + Slack consumers, `AddDiscordServices()`, `AddSlackServices()`
  ```csharp
  services.AddMassTransit(x => {
      x.AddConsumer<DiscordFixtureEventsHandler>();
      x.AddConsumer<SlackFixtureEventsHandler>();
      // ...all consumers, each gets its own ASB queue
      x.UsingAzureServiceBus((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
  });
  ```
- `EventPublishersModule` — `AddMassTransit` (no consumers, publish-only) + `AddFplWorkers()`
- `SearchIndexerModule` — `AddRecurringIndexer()`

`Program.cs` selects modules based on `--services`, builds `WebApplication` for WebApi or `IHost` for workers.

MassTransit always uses Azure Service Bus transport — no in-memory fallback. `ConnectionStrings__servicebus` must always be set, pointing to either real ASB or the local AlmostServiceBus emulator. The Aspire AppHost sets this automatically; without it, developers set it manually or run `dotnet almostservicebus` directly.

### Step 4 — Remove Azure Functions and Pulumi

`FplBot.Functions/FunctionEndpointTrigger.cs` is entirely commented out — nothing to preserve.

- Delete `src/FplBot.Functions/` and `src/FplBot.Infrastructure/`
- Remove from `src/FplBot.sln`
- Strip from `DeployToTest.yml` + `DeployToProd.yml`: remove `azure/login`, `Build Function`, `pulumi/actions`, `Deploy function` steps
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `PULUMI_ACCESS_TOKEN` secrets no longer needed (note in PR)

### Step 5 — Multi-stage Dockerfile

Replace `Dockerfile.web`, `Dockerfile.eventhandler`, `Dockerfile.eventpublisher`, `Dockerfile.indexer` with one `src/Dockerfile` using named final stages. BuildKit shares the `build` and `base` stages — one compile, 4 images. No platform-specific config files needed.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
ARG INFOVERSION="0.666"
ARG VERSION="1.0.666"
RUN dotnet publish FplBot -o /app/out -c Release \
    /p:Version=$VERSION /p:InformationalVersion=$INFOVERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=build /app/out .

FROM base AS web
CMD ["dotnet", "FplBot.dll", "--services", "WebApi"]

FROM base AS eventhandler
CMD ["dotnet", "FplBot.dll", "--services", "EventHandlers"]

FROM base AS eventpublisher
CMD ["dotnet", "FplBot.dll", "--services", "EventPublishers"]

FROM base AS indexer
CMD ["dotnet", "FplBot.dll", "--services", "SearchIndexer"]
```

### Step 6 — Update CI workflows

```bash
docker build --target web \
  --build-arg INFOVERSION=... --build-arg VERSION=... \
  -t registry.heroku.com/blank-fplbot-test/web -f ./src/Dockerfile ./src

docker build --target eventhandler ... -t registry.heroku.com/blank-fplbot-test/eventhandler ./src
docker build --target eventpublisher ... -t registry.heroku.com/blank-fplbot-test/eventpublisher ./src
docker build --target indexer ... -t registry.heroku.com/blank-fplbot-test/indexer ./src
```

BuildKit caches `build` and `base` across all four calls — only the first call does real compile work.

Release: `heroku container:release web eventhandler eventpublisher indexer`

Remove old `build-*.sh` scripts.

### Step 8 — Aspire AppHost for local dev

Create `src/FplBot.AppHost/FplBot.AppHost.csproj` (`net10.0`). **No Aspire packages in FplBot.csproj** — AppHost only sets env vars and launches processes.

AppHost NuGet packages: `Aspire.Hosting`, `Aspire.Hosting.Redis`, `AlmostServiceBus.Aspire.Hosting`

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("redis");
var serviceBus = builder.AddServiceBusEmulator("servicebus");

builder.AddProject<Projects.FplBot>("webapi")
    .WithArgs("--services", "WebApi")
    .WithReference(redis).WithReference(serviceBus);

builder.AddProject<Projects.FplBot>("eventhandlers")
    .WithArgs("--services", "EventHandlers")
    .WithReference(redis).WithReference(serviceBus);

builder.Build().Run();
```

Aspire injects `ConnectionStrings__redis` and `ConnectionStrings__servicebus` as env vars. MassTransit reads `ConnectionStrings__servicebus` for the ASB connection string automatically.

AlmostServiceBus connection string format:
```
Endpoint=sb://localhost:5672;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=emulator;UseDevelopmentEmulator=true
```

**Integration tests** — add to both test projects:
- `AlmostServiceBus.TestHost` — per-test ASB namespace isolation, no Docker required
- `Testcontainers.Redis` — real Redis per test run, no external dependency

Removes the `HEROKU_REDIS_COPPER_URL` CI secret dependency. Tests become fully self-contained.

### Step 9 — CI integration test infrastructure

The goal: `dotnet test src` works identically locally and in GitHub Actions — no pre-started services, no secrets, no manual setup.

**How it works:**

- `AlmostServiceBus.TestHost` runs **in-process** — no Docker, no port, no startup time. Each test class gets its own isolated ASB namespace via a unique `SharedAccessKeyName`. Fast and parallel-safe.
- `Testcontainers.Redis` starts a **real Redis container** automatically when the test suite runs. GitHub Actions `ubuntu-latest` has Docker — nothing to configure.

Both are added as NuGet packages to the test projects only. The `HEROKU_REDIS_COPPER_URL` CI secret is no longer needed and can be removed.

**`CI.yml` change**: remove the `HEROKU_REDIS_COPPER_URL` secret from the `dotnet test` step. The step stays otherwise identical — infrastructure spins up as part of `dotnet test`.

**`AlmostServiceBus.TestHost` IS the real emulator** — it runs in-process (full AMQP protocol, topics/subscriptions/filters), just embedded in the test process. Tests always use the real ASB semantics, never a fake. No in-memory shortcuts.

This is the same experience as running locally. No Aspire AppHost needed for tests — Aspire is for running the full app manually. Tests are self-contained.

### Step 10 — Update test projects

Update both test projects' `<ProjectReference>` to `../FplBot/FplBot.csproj`. Add `AlmostServiceBus.TestHost` and `Testcontainers.Redis`. Run `dotnet test src`.

### Step 11 — Delete old projects

Remove directories and solution entries for all migrated projects (see "Everything goes in" list above).

---

### Verification

1. `dotnet build src/FplBot` — clean build
2. `dotnet test src` — all green, self-contained (no external Redis or ASB)
3. `dotnet run --project src/FplBot --services WebApi` — starts on port 1337
4. `dotnet run --project src/FplBot --services EventHandlers` — MassTransit consumers start with in-memory transport (no ASB connection string set)
5. `dotnet run --project src/FplBot.AppHost` — Redis + AlmostServiceBus start, FplBot processes wire up to real transports
6. `docker build --target web -f src/Dockerfile ./src` — web image builds
7. CI deploy to test — all 4 Heroku dynos healthy

---

### Notes

- **`Fpl.Search.CommandLine`** — standalone maintenance CLI; fold into SearchIndexer module or keep separate (low priority)
- **Namespace changes** — keep existing namespaces during migration; cosmetic rename is a follow-up
- **`NSB_LICENSE` secret** — can be removed from GitHub secrets once MassTransit migration is verified in prod

