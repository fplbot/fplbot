using Fpl.Client.Models;

namespace FplBot.VerifiedEntries.Extensions;

public static class PlayerExtensions
{
    public static Player Get(this ICollection<Player> players, int? playerId)
    {
        return playerId.HasValue ? players.SingleOrDefault(p => p.Id == playerId) : null;
    }
}