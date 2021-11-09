using System.Text.Json.Serialization;


namespace Fpl.Client.Models;

public class EventEntryState
{
    [JsonPropertyName("event")]
    public int Event { get; set; }

    [JsonPropertyName("sub_state")]
    public string SubState { get; set; }

    [JsonPropertyName("event_day")]
    public int? EventDay { get; set; }

    [JsonPropertyName("deadline_time")]
    public DateTime? DeadlineTime { get; set; }

    [JsonPropertyName("deadline_time_formatted")]
    public string FormattedDeadlineTime { get; set; }
}