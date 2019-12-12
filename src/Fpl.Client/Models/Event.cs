using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Event
    {
        [JsonProperty("average_entry_score")]
        public int AverageEntryScore { get; set; }

        [JsonProperty("data_checked")]
        public bool DataChecked { get; set; }

        [JsonProperty("deadline_time")]
        public string DeadlineTime { get; set; }

        [JsonProperty("deadline_time_epoch")]
        public long DeadlineTimeEpoch { get; set; }

        [JsonProperty("deadline_time_formatted")]
        public string DeadlineTimeFormatted { get; set; }

        [JsonProperty("deadline_time_game_offset")]
        public int DeadlineTimeGameOffset { get; set; }

        [JsonProperty("finished")]
        public bool Finished { get; set; }

        [JsonProperty("highest_score")]
        public int? HighestScore { get; set; }

        [JsonProperty("highest_scoring_entry")]
        public int? HighestScoringEntry { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }

        [JsonProperty("is_next")]
        public bool IsNext { get; set; }

        [JsonProperty("is_previous")]
        public bool IsPrevious { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
