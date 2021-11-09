using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Gameweek
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("deadline_time")]
    public DateTime Deadline { get; set; }

    [JsonPropertyName("average_entry_score")]
    public double AverageScore { get; set; }

    [JsonPropertyName("finished")]
    public bool IsFinished { get; set; }

    [JsonPropertyName("data_checked")]
    public bool DateChecked { get; set; }

    [JsonPropertyName("highest_scoring_entry")]
    public int? HighestScoringEntryId { get; set; }

    [JsonPropertyName("deadline_time_epoch")]
    public int DeadLineTimeEpoch { get; set; }

    [JsonPropertyName("deadline_time_game_offset")]
    public int DeadLineTimeGameOffset { get; set; }

    [JsonPropertyName("deadline_time_formatted")]
    public string DeadLineTimeFormatted { get; set; }

    [JsonPropertyName("highest_score")]
    public int? HighestScore { get; set; }

    [JsonPropertyName("is_previous")]
    public bool IsPrevious { get; set; }

    [JsonPropertyName("is_current")]
    public bool IsCurrent { get; set; }

    [JsonPropertyName("is_next")]
    public bool IsNext { get; set; }
}