using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

[TimeToBeReceived("01:00:00")] // discard events not being handled within 1 hour
public record GameweekJustBegan(NewGameweek NewGameweek) : IEvent;

public record NewGameweek(int Id);
