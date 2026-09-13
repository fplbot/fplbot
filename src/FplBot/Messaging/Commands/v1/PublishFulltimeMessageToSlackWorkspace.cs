namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishFulltimeMessageToSlackWorkspace(string WorkspaceId, string ChannelId, string Title, string ThreadMessage);
