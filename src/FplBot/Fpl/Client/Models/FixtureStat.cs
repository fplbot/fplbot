using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class FixtureStat
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("a")]
    public ICollection<FixtureStatValue> AwayStats { get; set; }

    [JsonPropertyName("h")]
    public ICollection<FixtureStatValue> HomeStats { get; set; }
}
