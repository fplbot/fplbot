using Fpl.Client.Models;

namespace FplBot.Formatting.Helpers;

public interface IInjuredPlayersFinder
{
    IEnumerable<Player> FindInjuredPlayers(IEnumerable<Player> players);
}
