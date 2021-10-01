using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;

namespace FplBot.Slack.Extensions
{
    public static class GameweekExtensions
    {
        public static Gameweek GetCurrentGameweek(this ICollection<Gameweek> gameweeks)
        {
            return gameweeks.SingleOrDefault(x => x.IsCurrent);
        }
    }
}
