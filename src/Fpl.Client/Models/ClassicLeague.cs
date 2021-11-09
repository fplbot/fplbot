

using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class ClassicLeague
{
    [JsonPropertyName("new_entries")]
    public NewLeagueEntries NewEntries { get; set; }

    [JsonPropertyName("league")]
    public ClassicLeagueProperties Properties { get; set; }

    [JsonPropertyName("standings")]
    public ClassicLeagueStandings Standings { get; set; }

    [JsonPropertyName("detail")]
    public string Detail { get; set; }

    public bool Exists => Detail != "Not found.";
}