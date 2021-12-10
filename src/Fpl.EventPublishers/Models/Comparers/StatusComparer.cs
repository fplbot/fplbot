using Fpl.Client.Models;

namespace Fpl.EventPublishers.Models.Comparers;

public class StatusComparer : IEqualityComparer<Player>
{
    public bool Equals(Player x, Player y)
    {
        if (x == null && y == null)
            return true;
            
        if (x == null || y == null)
            return false;
            
        return x.Id == y.Id && x.Status == y.Status && x.News == y.News;
    }

    public int GetHashCode(Player obj)
    {
        return obj.Id;
    }
}
