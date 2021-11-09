using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record PlayersPriceChanged(List<PlayerWithPriceChange> PlayersWithPriceChanges) : IEvent;

public record PlayerWithPriceChange
(
    int PlayerId,
    string FirstName,
    string SecondName,
    int CostChangeEvent,
    int NowCost,
    double OwnershipPercentage,
    long TeamId,
    string TeamShortName
);