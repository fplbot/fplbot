using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.Helpers;

public interface IPriceChangedPlayersFinder
{
    IEnumerable<PlayerWithPriceChange> FindPriceChangedPlayers(ICollection<Player> players, ICollection<Team> teams);
}
