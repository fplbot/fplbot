using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class NewLeagueEntries
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("results")]
    public ICollection<NewLeagueEntry> Entries { get; set; }
}
