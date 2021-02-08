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
    }
}
