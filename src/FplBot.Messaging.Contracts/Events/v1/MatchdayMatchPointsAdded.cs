using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record MatchdayMatchPointsAdded(int Event, string Date) : IEvent;