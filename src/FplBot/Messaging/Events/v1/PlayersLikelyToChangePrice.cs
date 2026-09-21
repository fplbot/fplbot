namespace FplBot.Messaging.Contracts.Events.v1;

public record PlayersLikelyToChangePrice(List<PlayerLikelyPriceChange> Players);

public record PlayerLikelyPriceChange(
    int PlayerId,
    string WebName,
    int NowCost,
    long TeamId,
    string TeamShortName,
    string ProjectedPercent,
    int Likelihood);
