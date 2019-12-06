using Newtonsoft.Json;
using System.Collections.Generic;

namespace fplbot.consoleapp.Models
{
    public class ScoreBoard
    {
        public League League { get; set; }
        public StandingsWrapper Standings { get; set; }
    }

    public class League
    {
        public string Name { get; set; }
    }

    public class StandingsWrapper
    {
        public StandingsResult Standings { get; set; }
    }

    public class StandingsResult
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
