using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class HeadToHeadLeague
{
    [JsonPropertyName("new_entries")]
    public NewLeagueEntries NewEntries { get; set; }

    [JsonPropertyName("league")]
    public HeadToHeadLeagueProperties Properties { get; set; }

    [JsonPropertyName("standings")]
    public HeadToHeadLeagueStandings Standings { get; set; }

    [JsonPropertyName("matches_next")]
    public HeadToHeadLeagueMatches Next { get; set; }

    [JsonPropertyName("matches_this")]
    public HeadToHeadLeagueMatches Current { get; set; }
}
