using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record InjuryUpdateOccured(IEnumerable<InjuredPlayerUpdate> PlayersWithInjuryUpdates) : IEvent;

public record InjuredPlayerUpdate(InjuredPlayer Player, InjuryStatus Previous, InjuryStatus Updated);
public record InjuredPlayer(int PlayerId, string WebName, double OwnershipPercentage, TeamDescription Team);
public record InjuryStatus(string Status, string News);
public record TeamDescription(long TeamId, string ShortName, string Name);
