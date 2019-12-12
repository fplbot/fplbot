using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Gameweek
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deadline_time")]
        public DateTime Deadline { get; set; }

        [JsonProperty("average_entry_score")]
        public double AverageScore { get; set; }

        [JsonProperty("finished")]
        public bool IsFinished { get; set; }

        [JsonProperty("data_checked")]
        public bool DateChecked { get; set; }

        [JsonProperty("highest_scoring_entry")]
        public int? HighestScoringEntryId { get; set; }

        [JsonProperty("deadline_time_epoch")]
        public int DeadLineTimeEpoch { get; set; }

        [JsonProperty("deadline_time_game_offset")]
        public int DeadLineTimeGameOffset { get; set; }

        [JsonProperty("deadline_time_formatted")]
        public string DeadLineTimeFormatted { get; set; }

        [JsonProperty("highest_score")]
        public int? HighestScore { get; set; }

        [JsonProperty("is_previous")]
        public bool IsPrevious { get; set; }

        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }

        [JsonProperty("is_next")]
        public bool IsNext { get; set; }
    }
}
