using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class PlayerFixture
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("kickoff_time_formatted")]
    public string FormattedKickoffTime { get; set; }

    [JsonPropertyName("event_name")]
    public string EventName { get; set; }

    [JsonPropertyName("opponent_name")]
    public string OpponentName { get; set; }

    [JsonPropertyName("opponent_short_name")]
    public string OpponentShortName { get; set; }

    [JsonPropertyName("is_home")]
    public bool IsHome { get; set; }

    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("kickoff_time")]
    public DateTime? KickoffTime { get; set; }

    [JsonPropertyName("team_h_score")]
    public int? HomeTeamScore { get; set; }

    [JsonPropertyName("team_a_score")]
    public int? AwayTeamScore { get; set; }

    [JsonPropertyName("finished")]
    public bool Finished { get; set; }

    [JsonPropertyName("minutes")]
    public int Minutes { get; set; }

    [JsonPropertyName("provisional_start_time")]
    public bool ProvisionalStartTime { get; set; }

    [JsonPropertyName("finished_provisional")]
    public bool FinishedProvisional { get; set; }

    [JsonPropertyName("event")]
    public int? Event { get; set; }

    [JsonPropertyName("team_a")]
    public int AwayTeamId { get; set; }

    [JsonPropertyName("team_h")]
    public int HomeTeamId { get; set; }
}