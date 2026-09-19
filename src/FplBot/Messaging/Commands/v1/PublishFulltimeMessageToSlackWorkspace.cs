namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishFulltimeMessageToSlackWorkspace(string TeamId, string ChannelId, string Title, string ThreadMessage);
