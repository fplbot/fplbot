using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Transfer
{
    [JsonPropertyName("element_in")]
    public int ElementIn { get; set; }

    [JsonPropertyName("element_in_cost")]
    public int ElementInCost { get; set; }

    [JsonPropertyName("element_out")]
    public int ElementOut { get; set; }

    [JsonPropertyName("element_out_cost")]
    public int ElementOutCost { get; set; }

    [JsonPropertyName("entry")]
    public int Entry { get; set; }

    [JsonPropertyName("event")]
    public int Event { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
}