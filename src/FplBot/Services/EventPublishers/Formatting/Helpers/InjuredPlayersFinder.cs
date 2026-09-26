using Fpl.Client.Models;

namespace FplBot.Formatting.Helpers;

public class InjuredPlayersFinder : IInjuredPlayersFinder
{
    public IEnumerable<Player> FindInjuredPlayers(IEnumerable<Player> players)
    {
        return players.Where(p => p.OwnershipPercentage > 5 && IsInjured(p)).OrderByDescending(p => p.OwnershipPercentage);
    }

    private static bool IsInjured(Player player)
    {
        return player.ChanceOfPlayingNextRound.HasValue && player.ChanceOfPlayingNextRound != 100;
    }
}
