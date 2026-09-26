using Fpl.Client.Models;

namespace FplBot.Formatting.Helpers;

public interface IPlayerSearch
{
    Player? FindMostPopularMatchingPlayer(ICollection<Player> players, string name);
}
