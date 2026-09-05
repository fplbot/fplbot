namespace FplBot.Messaging.Contracts.Events.v1;

public record NewPlayersRegistered(List<NewPlayer> NewPlayers);

public record NewPlayer(int PlayerId, string WebName, int NowCost, long TeamId, string TeamShortName);
