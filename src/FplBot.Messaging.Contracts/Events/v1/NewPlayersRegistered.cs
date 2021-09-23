using System.Collections.Generic;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record NewPlayersRegistered(List<NewPlayer> NewPlayers) : IEvent;

    public record NewPlayer(int PlayerId, string WebName, int NowCost, long TeamId, string TeamShortName);
}
