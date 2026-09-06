namespace FplBot.Messaging.Contracts.Events.v1;

public record PlayersPriceChanged(List<PlayerWithPriceChange> PlayersWithPriceChanges);

public record PlayerWithPriceChange
(int PlayerId,
    string WebName,
    int CostChangeEvent,
    int NowCost,
    double OwnershipPercentage,
    long TeamId,
    string TeamShortName);
