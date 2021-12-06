using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishDeadlineNotificationToSlackWorkspace(string WorkspaceId,GameweekNearingDeadline Gameweek) : ICommand;
