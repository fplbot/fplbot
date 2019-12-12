using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EntryHistoryChip
    {
        [JsonProperty("played_time_formatted")]
        public string FormattedPlayedTime { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }

        [JsonProperty("chip")]
        public int Chip { get; set; }

        [JsonProperty("entry")]
        public int Entry { get; set; }

        [JsonProperty("event")]
        public int Event { get; set; }
    }
}
