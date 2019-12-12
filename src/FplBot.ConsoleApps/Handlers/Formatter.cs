using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fpl.Client.Models;

namespace FplBot.ConsoleApps.Handlers
{
    public class Formatter
    {
        public static string GetStandings(ClassicLeague league, ICollection<Gameweek> gameweeks)
        {
            var sb = new StringBuilder();

            var sortedByRank = league.Standings.Entries.OrderBy(x => x.Rank);

            var numPlayers = league.Standings.Entries.Count();

            var currentGw = gameweeks.SingleOrDefault(x => x.IsCurrent)?.Id.ToString() ?? "?";

            
            sb.Append($":star: *Resultater etter GW {currentGw}* :star: \n\n");

            foreach (var player in sortedByRank)
            {
                var arrow = GetRankChangeEmoji(player, numPlayers);
                sb.Append($"{player.Rank}. <https://fantasy.premierleague.com/entry/{player.Id}/event/{currentGw}|{player.EntryName}> - {player.Total} {arrow} \n");
            }

            return sb.ToString();
        }

        private static string GetRankChangeEmoji(ClassicLeagueEntry player, int numPlayers)
        {
            var rankDiff = player.LastRank - player.Rank;

            var emojiString = new StringBuilder();

            if (rankDiff < 0)
            {
                emojiString.Append($":chart_with_downwards_trend: ({rankDiff}) ");
            }

            if (rankDiff > 0)
            {
                emojiString.Append($":chart_with_upwards_trend: (+{rankDiff}) ");
            }

            if (player.Rank == numPlayers)
            {
                emojiString.Append(":rip:");
            }

            return emojiString.ToString();
        }
    }
}
