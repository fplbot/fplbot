using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FplBot.ConsoleApps.Models
{
    public class ScoreBoard
    {
        public League League { get; set; }
        public Standings Standings { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            var sortedByRank = Standings.Results.OrderBy(x => x.Rank);

            var numPlayers = Standings.Results.Count();

            foreach (var player in sortedByRank)
            {
                var arrow = GetRankChangeEmoji(player, numPlayers);
                sb.Append($"{player.Rank}. *{player.TeamName}* - {player.TotalPoints} {arrow}\n");
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
                emojiString.Append($":chart_with_upwards_trend: ({rankDiff}) ");
            }

            if (player.Rank == numPlayers)
            {
                emojiString.Append(":rip:");
            }

            return emojiString.ToString();
        }
    }

    public class League
    {
        public string Name { get; set; }
    }

    public class Standings
    {
        public IEnumerable<Player> Results { get; set; }
    }

    public class Player
    {
        [JsonProperty("player_name")]
        public string Name { get; set; }
        [JsonProperty("entry_name")]
        public string TeamName { get; set; }
        [JsonProperty("entry")]
        public int Id { get; set; }
        [JsonProperty("rank")]
        public int Rank { get; set; }
        [JsonProperty("last_rank")]
        public int LastRank { get; set; }
        [JsonProperty("total")]
        public int TotalPoints { get; set; }
    }
}
