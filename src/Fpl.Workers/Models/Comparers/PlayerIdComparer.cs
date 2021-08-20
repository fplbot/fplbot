using System.Collections.Generic;
using Fpl.Client.Models;

namespace FplBot.Core.Helpers.Comparers
{
    public class PlayerIdComparer : IEqualityComparer<Player>
    {
        public bool Equals(Player x, Player y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            return x.Id == y.Id;
        }

        public int GetHashCode(Player obj)
        {
            return obj.Id;
        }
    }
}
