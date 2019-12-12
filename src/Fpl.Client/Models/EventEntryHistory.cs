using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EventEntryHistory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("movement")]
        public string Movement { get; set; }

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

        [JsonProperty("targets")]
        public string Targets { get; set; }

        [JsonProperty("event_transfers")]
        public int EventTransfers { get; set; }

        [JsonProperty("event_transfers_cost")]
        public int EventTransfersCost { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("points_on_bench")]
        public int PointsOnBench { get; set; }

        [JsonProperty("bank")]
        public int Bank { get; set; }

        [JsonProperty("entry")]
        public int Entry { get; set; }

        [JsonProperty("event")]
        public int Event { get; set; }
    }
}
