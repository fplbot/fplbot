using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record FixtureRemovedFromGameweek(int Gameweek, RemovedFixture RemovedFixture) : IEvent;

public record RemovedFixture(int Id, RemovedTeam Home, RemovedTeam Away);

public record RemovedTeam(int Id, string Name, string ShortName);
