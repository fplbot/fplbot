using Fpl.Client.Models;
using System.Collections.Generic;
using System.Linq;

namespace Slackbot.Net.Extensions.FplBot.Extensions
{
    public static class GameweekExtensions
    {
        public static Gameweek GetCurrentGameweek(this ICollection<Gameweek> gameweeks)
        {
            return gameweeks.SingleOrDefault(x => x.IsCurrent);
        }
    }
}