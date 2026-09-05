using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Phase
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("start_event")]
    public long StartEvent { get; set; }

    [JsonPropertyName("stop_event")]
    public long StopEvent { get; set; }
}
