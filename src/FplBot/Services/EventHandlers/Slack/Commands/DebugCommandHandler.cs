using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack;
using FplBot.Services.WebApi.Slack.Helpers;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class DebugCommandHandler(ISlackWorkSpacePublisher publisher) : IConsumer<ProcessDebugCommand>
{
    public async Task Consume(ConsumeContext<ProcessDebugCommand> context)
    {
        var command = context.Message;
        var debugDetails = MetaService.DebugInfo();
        var debugInfo = $"▪️ v{debugDetails.MajorMinorPatch}\n" +
                        $"▪️ {debugDetails.Informational}\n";
        var releaseNotes = await GitHubReleaseService.GetReleaseNotes(debugDetails.MajorMinorPatch);
        if (!string.IsNullOrEmpty(releaseNotes))
        {
            debugInfo += releaseNotes;
        }
        else if (debugDetails.Sha != "0")
        {
            debugInfo += $"️▪️ <https://github.com/fplbot/fplbot/tree/{debugDetails.Sha}|{debugDetails.Sha?.Substring(0, debugDetails.Sha.Length - 1)}>\n";
        }

        await publisher.PublishToWorkspace(command.TeamId, command.Channel, debugInfo);
    }
}
