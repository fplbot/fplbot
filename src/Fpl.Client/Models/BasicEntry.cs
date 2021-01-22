using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class BasicEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("joined_time")]
        public DateTime JoinedTime { get; set; }

        [JsonProperty("started_event")]
        public int StartEvent { get; set; }

        [JsonProperty("favourite_team")]
        public int? FavouriteTeamId { get; set; }

        [JsonProperty("player_first_name")]
        public string PlayerFirstName { get; set; }

        [JsonProperty("player_last_name")]
        public string PlayerLastName { get; set; }

        [JsonProperty("player_region_id")]
        public int PlayerRegionId { get; set; }

        [JsonProperty("player_region_name")]
        public string PlayerRegionName { get; set; }

        [JsonProperty("player_region_iso_code_short")]
        public string PlayerRegionShortIso { get; set; }
        
        [JsonProperty("player_region_iso_code_long")]
        public string PlayerRegionLongIso { get; set; }

        [JsonProperty("summary_overall_points")]
        public int? SummaryOverallPoints { get; set; }

        [JsonProperty("summary_overall_rank")]
        public int? SummaryOverallRank { get; set; }

        [JsonProperty("summary_event_points")]
        public int? SummaryEventPoints { get; set; }

        [JsonProperty("summary_event_rank")]
        public int? SummaryEventRank { get; set; }

        [JsonProperty("current_event")]
        public int? CurrentEvent { get; set; }

        [JsonProperty("leagues")]
        public EntryLeagues Leagues { get; set; }

        [JsonProperty("name")]
        public string TeamName { get; set; }

        [JsonProperty("kit")]
        public string Kit { get; set; }

        public string PlayerFullName => $"{PlayerFirstName} {PlayerLastName}";
    }
}
