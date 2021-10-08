using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Discord.Extensions
{
    public static class PlayerExtensions
    {
        public static bool IsRelevant(this Player player)
        {
            return player.OwnershipPercentage > 7;
        }

        public static bool IsRelevant(this InjuredPlayer player)
        {
            return player.OwnershipPercentage > 7;
        }

        public static Player Get(this ICollection<Player> players, int? playerId)
        {
            return playerId.HasValue ? players.SingleOrDefault(p => p.Id == playerId) : null;
        }
    }
}
