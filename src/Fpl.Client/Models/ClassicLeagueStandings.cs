using System.Text.Json.Serialization;


namespace Fpl.Client.Models;

public class ClassicLeagueStandings
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("results")]
    public ICollection<ClassicLeagueEntry> Entries { get; set; }
}