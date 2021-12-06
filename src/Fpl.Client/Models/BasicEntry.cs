using System.Text.Json.Serialization;


namespace Fpl.Client.Models;

public class BasicEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("joined_time")]
    public DateTime JoinedTime { get; set; }

    [JsonPropertyName("started_event")]
    public int StartEvent { get; set; }

    [JsonPropertyName("favourite_team")]
    public int? FavouriteTeamId { get; set; }

    [JsonPropertyName("player_first_name")]
    public string PlayerFirstName { get; set; }

    [JsonPropertyName("player_last_name")]
    public string PlayerLastName { get; set; }

    [JsonPropertyName("player_region_id")]
    public int PlayerRegionId { get; set; }

    [JsonPropertyName("player_region_name")]
    public string PlayerRegionName { get; set; }

    [JsonPropertyName("player_region_iso_code_short")]
    public string PlayerRegionShortIso { get; set; }

    [JsonPropertyName("player_region_iso_code_long")]
    public string PlayerRegionLongIso { get; set; }

    [JsonPropertyName("summary_overall_points")]
    public int? SummaryOverallPoints { get; set; }

    [JsonPropertyName("summary_overall_rank")]
    public int? SummaryOverallRank { get; set; }

    [JsonPropertyName("summary_event_points")]
    public int? SummaryEventPoints { get; set; }

    [JsonPropertyName("summary_event_rank")]
    public int? SummaryEventRank { get; set; }

    [JsonPropertyName("current_event")]
    public int? CurrentEvent { get; set; }

    [JsonPropertyName("leagues")]
    public EntryLeagues Leagues { get; set; }

    [JsonPropertyName("name")]
    public string TeamName { get; set; }

    [JsonPropertyName("kit")]
    public string Kit { get; set; }

    public string PlayerFullName => $"{PlayerFirstName} {PlayerLastName}";

    [JsonPropertyName("detail")]
    public string Detail { get; set; }
    public bool Exists => Detail != "Not found.";
}
