using Newtonsoft.Json;
using System.Collections.Generic;

namespace FplBot.ConsoleApps.Models
{
    public class PlayerStats
    {
        public IEnumerable<GameWeek> Current { get; set; }
    }

    public class GameWeek
    {
        public int Event { get; set; }
        [JsonProperty("event_transfers")]
        public int Transfers { get; set; }
        [JsonProperty("overall_rank")]
        public int OverallRank { get; set; }
        public int Points { get; set; }
        [JsonProperty("total_points")]
        public int TotalPoints { get; set; }
        [JsonProperty("value")]
        public int TeamValue { get; set; }
        public int Bank { get; set; }
    }
}
