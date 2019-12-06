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

            foreach (var player in sortedByRank)
            {
                var arrow = GetRankChangeEmoji(player);
                sb.Append($"{player.Rank}. - {player.TeamName} ({player.TotalPoints}) {arrow}\n");
            }

            return sb.ToString();
        }

        private static string GetRankChangeEmoji(Player player)
        {
            var rankDiff = player.LastRank - player.Rank;
            
            if (rankDiff == 0)
            {
                return ":caps:";
            }

            if (rankDiff < 0)
            {
                return ":alv:";
            }

            if (rankDiff > 0)
            {
                return ":knut:";
            }

            return null;
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
