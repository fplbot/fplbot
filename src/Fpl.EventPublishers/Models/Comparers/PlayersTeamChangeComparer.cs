using Fpl.Client.Models;

namespace Fpl.EventPublishers.Models.Comparers;

public class PlayersTeamChangeComparer : IEqualityComparer<Player>
{
    public bool Equals(Player x, Player y)
    {
        if (x == null && y == null)
            return true;

        if (x == null || y == null)
            return false;

        return x.Id == y.Id && x.TeamCode == y.TeamCode;
    }

    public int GetHashCode(Player obj)
    {
        return obj.Id;
    }
}
