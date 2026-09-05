

using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Event
{
    [JsonPropertyName("average_entry_score")]
    public int AverageEntryScore { get; set; }

    [JsonPropertyName("data_checked")]
    public bool DataChecked { get; set; }

    [JsonPropertyName("deadline_time")]
    public string DeadlineTime { get; set; }

    [JsonPropertyName("deadline_time_epoch")]
    public long DeadlineTimeEpoch { get; set; }

    [JsonPropertyName("deadline_time_formatted")]
    public string DeadlineTimeFormatted { get; set; }

    [JsonPropertyName("deadline_time_game_offset")]
    public int DeadlineTimeGameOffset { get; set; }

    [JsonPropertyName("finished")]
    public bool Finished { get; set; }

    [JsonPropertyName("highest_score")]
    public int? HighestScore { get; set; }

    [JsonPropertyName("highest_scoring_entry")]
    public int? HighestScoringEntry { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("is_current")]
    public bool IsCurrent { get; set; }

    [JsonPropertyName("is_next")]
    public bool IsNext { get; set; }

    [JsonPropertyName("is_previous")]
    public bool IsPrevious { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
