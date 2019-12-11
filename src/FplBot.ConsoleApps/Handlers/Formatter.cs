using System.Linq;
using System.Text;
using Fpl.Client.Models;

namespace FplBot.ConsoleApps
{
    public class Formatter
    {
        public static string GetStandings(ScoreBoard scoreBoard, Bootstrap bootstrap)
        {
            var sb = new StringBuilder();

            var sortedByRank = scoreBoard.Standings.Results.OrderBy(x => x.Rank);

            var numPlayers = scoreBoard.Standings.Results.Count();

            var currentGw = bootstrap.Events.SingleOrDefault(x => x.IsCurrent)?.Id.ToString() ?? "?";

            
            sb.Append($":star: *Resultater etter GW {currentGw}* :star: \n\n");

            foreach (var player in sortedByRank)
            {
                var arrow = GetRankChangeEmoji(player, numPlayers);
                sb.Append($"{player.Rank}. <https://fantasy.premierleague.com/entry/{player.Id}/event/{currentGw}|{player.TeamName}> - {player.TotalPoints} {arrow} \n");
            }

            return sb.ToString();
        }

        private static string GetRankChangeEmoji(Player player, int numPlayers)
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
