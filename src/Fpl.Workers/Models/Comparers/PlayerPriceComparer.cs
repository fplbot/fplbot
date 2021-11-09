using Fpl.Client.Models;

namespace Fpl.Workers.Models.Comparers;

public class PlayerPriceComparer : IEqualityComparer<Player>
{
    public bool Equals(Player x, Player y)
    {
        if (x == null && y == null)
            return true;

        if (x == null || y == null)
            return false;

        return x.Id == y.Id && x.CostChangeEvent == y.CostChangeEvent;
    }

    public int GetHashCode(Player obj)
    {
        return obj.Id;
    }
}