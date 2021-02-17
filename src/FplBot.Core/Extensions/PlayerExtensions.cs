using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;

namespace FplBot.Core.Extensions
{
    public static class PlayerExtensions
    {
        public static bool IsRelevant(this Player player)
        {
            if (player.OwnershipPercentage > 7)
            {
                return true;
            }

            return false;
        }

        public static Player Get(this ICollection<Player> players, int? playerId)
        {
            return playerId.HasValue ? players.SingleOrDefault(p => p.Id == playerId) : null;
        }
    }
}
