using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EventEntryHistory
    {
        [JsonProperty("event")]
        public int Event { get; set; }
        
        [JsonProperty("points")]
        public int Points { get; set; }

        [JsonProperty("total_points")]
        public int TotalPoints { get; set; }

        [JsonProperty("rank")]
        public int? Rank { get; set; }

        [JsonProperty("rank_sort")]
        public string RankSort { get; set; }

        [JsonProperty("overall_rank")]
        public int? OverallRank { get; set; }
        
        [JsonProperty("bank")]
        public int Bank { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("event_transfers")]
        public int EventTransfers { get; set; }

        [JsonProperty("event_transfers_cost")]
        public int EventTransfersCost { get; set; }

        [JsonProperty("points_on_bench")]
        public int PointsOnBench { get; set; }
    }
}
