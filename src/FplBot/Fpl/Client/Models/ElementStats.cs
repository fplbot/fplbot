using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class ElementStats
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
