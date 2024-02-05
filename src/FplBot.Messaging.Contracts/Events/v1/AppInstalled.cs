using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record AppInstalled(string TeamId, string TeamName, ChatPlatform Platform) : IEvent;

public enum ChatPlatform { Unknown, Slack, Discord }
