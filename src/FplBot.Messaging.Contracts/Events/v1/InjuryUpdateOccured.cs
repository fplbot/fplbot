using System.Collections.Generic;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record InjuryUpdateOccured(IEnumerable<InjuredPlayerUpdate> PlayersWithInjuryUpdates) : IEvent;

    public record InjuredPlayerUpdate(InjuredPlayer Player, InjuryStatus PreviousStatus, InjuryStatus UpdatedStatus, TeamDescription Team);
    public record InjuredPlayer(int PlayerId, string WebName, double OwnershipPercentage);
    public record InjuryStatus(string Status, string News);
    public record TeamDescription(long TeamId, string ShortName, string Name);
}
