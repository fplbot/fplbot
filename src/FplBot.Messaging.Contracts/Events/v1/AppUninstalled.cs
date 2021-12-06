using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record AppUninstalled(string TeamId, string TeamName) : IEvent;
