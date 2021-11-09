using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record GameweekFinished(FinishedGameweek FinishedGameweek) : IEvent;

public record FinishedGameweek(int Id);