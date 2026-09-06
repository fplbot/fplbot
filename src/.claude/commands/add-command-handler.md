# Add a new chat command handler

This skill adds a new slash/mention command that users can invoke directly in Slack or Discord (e.g. `@fplbot injuries`, `/fplbot captains`).

## Slack mention commands (`@fplbot <command>`)

### 1. Create the handler

Create `FplBot/Services/WebApi/Slack/Handlers/SlackEvents/FplYourCommandHandler.cs`:

```csharp
using FplBot.WebApi.Slack.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

internal class FplYourCommandHandler : HandleAppMentionBase
{
    private readonly ISlackWorkSpacePublisher _publisher;
    // inject other dependencies (FPL clients, repos) as needed

    public FplYourCommandHandler(ISlackWorkSpacePublisher publisher)
    {
        _publisher = publisher;
    }

    // Keywords that trigger this handler (user types: @fplbot yourcommand)
    public override string[] Commands => ["yourcommand"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        var text = BuildResponseText(message.Text);

        if (string.IsNullOrEmpty(text))
            return new EventHandledResponse("Nothing found");

        await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, text);
        return new EventHandledResponse(text);
    }

    private string BuildResponseText(string rawMessage)
    {
        // Parse arguments from rawMessage if needed
        return "Your response here";
    }

    // Shown in the @fplbot help output
    public override (string, string) GetHelpDescription() => (CommandsFormatted, "Short description of what this does");
}
```

### 2. Register the handler

Open `FplBot/Services/WebApi/Slack/ServiceCollectionExtensions.cs` and add a line inside `AddFplBotSlackWebEndpoints`:

```csharp
.AddAppMentionHandler<FplYourCommandHandler>()
```

Place it before `.AddNoOpAppMentionHandler<UnknownAppMentionCommandHandler>()`.

### 3. Add tests

Create `FplBot.Tests/FplYourCommandHandlerTests.cs`. Look at `FplBot.Tests/FplInjuryCommandHandlerTests.cs` or `FplPlayerCommandHandlerTests.cs` for the pattern — inject fakes via `Factory.cs`.

---

## Discord slash commands

Discord commands are registered differently — they're registered as application commands via the Discord API.

Look at `FplBot/Services/WebApi/Discord/` for the existing slash command setup:
- `DiscordSlashCommandsEnsurer.cs` — registers commands with Discord
- Endpoint handlers parse the incoming interaction payload

For a new Discord command:
1. Add the command definition in `DiscordSlashCommandsEnsurer.cs`
2. Add a handler in the Discord interactions endpoint that matches the command name
3. Respond via `IPublishEndpoint` publishing a `PublishToGuildChannel` command (goes through MassTransit)

---

## Tips

- **Argument parsing**: Extract text from `message.Text` in Slack (the raw mention text). Use simple string splitting for keyword-based args.
- **Fuzzy matching**: For player name searches, look at `FplPlayerCommandHandler` which uses Levenshtein distance via `Fastenshtein`.
- **Thread replies**: Publish `PublishSlackThreadMessage` instead of `PublishToWorkspace` to reply in a thread.
- **Formatting**: Extract complex formatting logic to `FplBot/Formatting/` and test it separately — formatters are pure functions and easy to unit test.

## Verify

```bash
dotnet build src/FplBot
dotnet test src
```
