using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record MatchdayBonusPointsAdded(int Event, string Date) : IEvent;
