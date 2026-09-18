# Add a new chat command handler

This skill adds a slash/mention command users invoke directly in Slack or Discord (e.g. `@fplbot injuries`, `/fplbot captains`).

## The shape

Commands are **two-stage** since #459, and both stages are required:

1. **WebApi** — the mention/slash handler parses the incoming payload and does nothing but publish a `Process<Name>Command` via `IPublishEndpoint`. No FPL calls, no repository work, no posting.
2. **EventHandlers** — an `IConsumer<Process<Name>Command>` in `Services/EventHandlers/{Slack,Discord}/Commands/` does the work and posts the reply.

This keeps the webhook response fast (Slack and Discord both time out in ~3s) and puts the work on the bus where it retries and is visible in the error queues.

---

## Slack mention commands (`@fplbot <command>`)

### 1. Define the command contract

`FplBot/Messaging/Commands/v1/ProcessYourCommand.cs`:

```csharp
namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessYourCommand(string TeamId, string Channel);
```

### 2. Add the help entry

Add a `CommandHelp` to `FplBot/ApplicationServices/Slack/SlackCommandCatalog.cs` and include it in `All`:

```csharp
public static readonly CommandHelp YourCommand = new("yourcommand", "Short description of what this does");
```

### 3. Create the WebApi mention handler

`FplBot/Services/WebApi/Slack/Handlers/SlackEvents/AppMentions/FplYourCommandHandler.cs`:

```csharp
using FplBot.ApplicationServices.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

internal class FplYourCommandHandler(IPublishEndpoint publishEndpoint) : HandleAppMentionBase
{
    public override string[] Commands => ["yourcommand"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        await publishEndpoint.Publish(new ProcessYourCommand(eventMetadata.Team_Id, message.Channel));
        return new EventHandledResponse("OK");
    }

    public override (string, string) GetHelpDescription() =>
        (SlackCommandCatalog.YourCommand.Trigger, SlackCommandCatalog.YourCommand.Description);
}
```

If the command takes arguments, parse them out of `message.Text` here and put them on the record.

### 4. Register the mention handler

In `FplBot/Services/WebApi/Slack/ServiceCollectionExtensions.cs`, inside `AddFplBotSlackWebEndpoints`:

```csharp
.AddAppMentionHandler<FplYourCommandHandler>()
```

Place it before `.AddNoOpAppMentionHandler<UnknownAppMentionCommandHandler>()`.

### 5. Create the consumer that does the work

`FplBot/Services/EventHandlers/Slack/Commands/YourCommandHandler.cs`:

```csharp
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class YourCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    IGlobalSettingsClient globalSettingsClient)
    : IConsumer<ProcessYourCommand>
{
    public async Task Consume(ConsumeContext<ProcessYourCommand> context)
    {
        var command = context.Message;
        var settings = await globalSettingsClient.GetGlobalSettings();

        var textToSend = Formatter.FormatYourThing(settings);
        if (string.IsNullOrEmpty(textToSend))
            return;

        await workspacePublisher.PublishToWorkspace(command.TeamId, command.Channel, textToSend);
    }
}
```

Always post through `ISlackWorkSpacePublisher` — never `ISlackClientBuilder.Build(token).ChatPostMessage(...)`. The publisher owns delivery-failure accounting, so a direct post leaves a dead channel with no way to self-heal. Non-posting client calls (`UsersList`, `ConversationsMembers`) may still use `ISlackClientBuilder`.

### 6. Register the consumer — CRITICAL

In `FplBot/Services/EventHandlers/EventHandlersService.cs`:

```csharp
cfg.AddConsumer<YourCommandHandler>();
```

Forgetting this means the command silently does nothing — no error.

---

## Discord slash commands

Same two stages, different plumbing.

1. Add the command (and any subcommand/options) to `FplBot/Services/WebApi/Discord/DiscordSlashCommandsEnsurer.cs`, which registers application commands with Discord at startup.
2. Add an `ISlashCommandHandler` in `Services/WebApi/Discord/Handlers/SlashCommands/` that publishes a `Process<Name>Command` and returns a `DeferredResponse`. Pass `context.InteractionToken` on the command so the consumer can send the followup, and check `ChannelPermissions.Problem(context.AppPermissions)` before deferring.
3. Add the consumer in `Services/EventHandlers/Discord/Commands/`, responding via `PublishRichToGuildChannel` / the interaction followup.
4. Register the consumer in `EventHandlersService.ConfigureMassTransit()`.

Exercising the real interaction followup needs the **Integration** environment — in Development `DevLoggingDiscordClient` swallows it and the command sits on "thinking…" forever.

---

## Tips

- **Argument parsing** belongs in the WebApi handler; the command record carries already-parsed values.
- **Fuzzy matching**: `FplPlayerCommandHandler` / `PlayerCommandHandler` use Levenshtein distance via `Fastenshtein`.
- **Thread replies**: publish `PublishSlackThreadMessage` instead of `PublishToSlack`.
- **Formatting** goes in `Services/EventPublishers/Formatting/` (namespace `FplBot.Formatting`); builders stay static and synchronous, fed already-fetched data.

## Tests — required, not optional

Add an **E2E test** in `FplBot.Tests/E2E/` that drives the command from the real entry point — `fixture.AskSlackbot(teamId, channel, "<@UREFQD887> yourcommand")` — and asserts the message that comes back out of the capturing Slack/Discord client. Set state up via `InstallSlackbot()` / `Subscribe(...)`, assert on outcomes, and don't use `A.CallTo()` assertions on internals.

## Verify

```bash
dotnet build --no-incremental src/FplBot/FplBot.csproj
dotnet run --project src/Build -- test
```
