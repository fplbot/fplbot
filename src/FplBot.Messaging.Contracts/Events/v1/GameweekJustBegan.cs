using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record GameweekJustBegan(NewGameweek NewGameweek) : IEvent;

public record NewGameweek(int Id);