using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishDeadlineNotificationToSlackWorkspace(string TeamId, string ChannelId, GameweekNearingDeadline Gameweek);
