namespace Fpl.Client.Models;

public static class GameweekExtensions
{
    public static Gameweek? GetCurrentGameweek(this ICollection<Gameweek> gameweeks)
    {
        return gameweeks.SingleOrDefault(x => x.IsCurrent);
    }

    public static Gameweek? GetPreviousGameweek(this ICollection<Gameweek> gameweeks)
    {
        return gameweeks.SingleOrDefault(x => x.IsPrevious);
    }

    public static Gameweek? GetNextGameweek(this ICollection<Gameweek> gameweeks)
    {
        return gameweeks.SingleOrDefault(x => x.IsNext);
    }
}
