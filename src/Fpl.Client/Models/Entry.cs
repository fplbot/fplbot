using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Entry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("player_first_name")]
        public string PlayerFirstName { get; set; }

        [JsonProperty("player_last_name")]
        public string PlayerLastName { get; set; }

        [JsonProperty("player_region_id")]
        public int PlayerRegionId { get; set; }

        [JsonProperty("player_region_name")]
        public string PlayerRegionName { get; set; }

        [JsonProperty("player_region_short_iso")]
        public string PlayerRegionShortIso { get; set; }

        [JsonProperty("summary_overall_points")]
        public int? SummaryOverallPoints { get; set; }

        [JsonProperty("summary_overall_rank")]
        public int? SummaryOverallRank { get; set; }

        [JsonProperty("summary_event_points")]
        public int? SummaryEventPoints { get; set; }

        [JsonProperty("summary_event_rank")]
        public int? SummaryEventRank { get; set; }

        [JsonProperty("joined_seconds")]
        public long JoinedSeconds { get; set; }

        [JsonProperty("current_event")]
        public int CurrentEvent { get; set; }

        [JsonProperty("total_transfers")]
        public int TotalTransfers { get; set; }

        [JsonProperty("total_loans")]
        public int TotalLoans { get; set; }

        [JsonProperty("total_loans_active")]
        public int TotalLoansActive { get; set; }

        [JsonProperty("transfers_or_loans")]
        public string TransfersOrLoans { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("Email")]
        public bool Email { get; set; }

        [JsonProperty("joined_time")]
        public DateTime JoinedTime { get; set; }

        [JsonProperty("name")]
        public string TeamName { get; set; }

        [JsonProperty("bank")]
        public int Bank { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("kit")]
        public string Kit { get; set; }

        [JsonProperty("event_transfers")]
        public int EventTransfers { get; set; }

        [JsonProperty("event_transfers_cost")]
        public int EventTransfersCost { get; set; }

        [JsonProperty("extra_free_transfers")]
        public int ExtraFreeTransfers { get; set; }

        [JsonProperty("strategy")]
        public string Strategy { get; set; }

        [JsonProperty("favourite_team")]
        public int? FavouriteTeamId { get; set; }

        [JsonProperty("started_event")]
        public int StartEvent { get; set; }

        [JsonProperty("player")]
        public int Player { get; set; }
    }
}
