using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public record PublishFulltimeMessageToSlackWorkspace(string WorkspaceId, string Title, string ThreadMessage): ICommand;
}
