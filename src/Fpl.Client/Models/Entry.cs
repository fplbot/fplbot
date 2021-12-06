using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Entry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("player_first_name")]
    public string PlayerFirstName { get; set; }

    [JsonPropertyName("player_last_name")]
    public string PlayerLastName { get; set; }

    [JsonPropertyName("player_region_id")]
    public int PlayerRegionId { get; set; }

    [JsonPropertyName("player_region_name")]
    public string PlayerRegionName { get; set; }

    [JsonPropertyName("player_region_short_iso")]
    public string PlayerRegionShortIso { get; set; }

    [JsonPropertyName("summary_overall_points")]
    public int? SummaryOverallPoints { get; set; }

    [JsonPropertyName("summary_overall_rank")]
    public int? SummaryOverallRank { get; set; }

    [JsonPropertyName("summary_event_points")]
    public int? SummaryEventPoints { get; set; }

    [JsonPropertyName("summary_event_rank")]
    public int? SummaryEventRank { get; set; }

    [JsonPropertyName("joined_seconds")]
    public long JoinedSeconds { get; set; }

    [JsonPropertyName("current_event")]
    public int CurrentEvent { get; set; }

    [JsonPropertyName("total_transfers")]
    public int TotalTransfers { get; set; }

    [JsonPropertyName("total_loans")]
    public int TotalLoans { get; set; }

    [JsonPropertyName("total_loans_active")]
    public int TotalLoansActive { get; set; }

    [JsonPropertyName("transfers_or_loans")]
    public string TransfersOrLoans { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("Email")]
    public bool Email { get; set; }

    [JsonPropertyName("joined_time")]
    public DateTime JoinedTime { get; set; }

    [JsonPropertyName("name")]
    public string TeamName { get; set; }

    [JsonPropertyName("bank")]
    public int Bank { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("kit")]
    public string Kit { get; set; }

    [JsonPropertyName("event_transfers")]
    public int EventTransfers { get; set; }

    [JsonPropertyName("event_transfers_cost")]
    public int EventTransfersCost { get; set; }

    [JsonPropertyName("extra_free_transfers")]
    public int ExtraFreeTransfers { get; set; }

    [JsonPropertyName("strategy")]
    public string Strategy { get; set; }

    [JsonPropertyName("favourite_team")]
    public int? FavouriteTeamId { get; set; }

    [JsonPropertyName("started_event")]
    public int StartEvent { get; set; }

    [JsonPropertyName("player")]
    public int Player { get; set; }
}
