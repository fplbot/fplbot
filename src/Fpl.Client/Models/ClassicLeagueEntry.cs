using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class ClassicLeagueEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("entry_name")]
        public string EntryName { get; set; }

        [JsonProperty("event_total")]
        public int EventTotal { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }

        [JsonProperty("movement")]
        public string Movement { get; set; }

        [JsonProperty("own_entry")]
        public bool OwnEntry { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("last_rank")]
        public int LastRank { get; set; }

        [JsonProperty("rank_sort")]
        public int RankSort { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("entry")]
        public int Entry { get; set; }

        [JsonProperty("league")]
        public int League { get; set; }

        [JsonProperty("start_event")]
        public int StartEvent { get; set; }

        [JsonProperty("stop_event")]
        public int StopEvent { get; set; }
    }
}
