using System.Text.Json.Serialization;


namespace Fpl.Client.Models;

public class EntryHistoryChip
{
    [JsonPropertyName("played_time_formatted")]
    public string FormattedPlayedTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("chip")]
    public int Chip { get; set; }

    [JsonPropertyName("entry")]
    public int Entry { get; set; }

    [JsonPropertyName("event")]
    public int Event { get; set; }
}