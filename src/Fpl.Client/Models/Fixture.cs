using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Fixture
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("kickoff_time_formatted")]
        public string FormattedKickOffTime { get; set; }

        [JsonProperty("started")]
        public bool? Started { get; set; }

        [JsonProperty("event_day")]
        public bool? EventDay { get; set; }

        [JsonProperty("deadline_time")]
        public DateTime? DeadlineTime { get; set; }

        [JsonProperty("deadline_time_formatted")]
        public string FormattedDeadlineTime { get; set; }

        [JsonProperty("stats")]
        public FixtureStat[] Stats { get; set; }

        [JsonProperty("team_h_difficulty")]
        public int HomeTeamDifficulty { get; set; }

        [JsonProperty("team_a_difficulty")]
        public int AwayTeamDifficulty { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("kickoff_time")]
        public DateTime? KickOffTime { get; set; }

        [JsonProperty("team_h_score")]
        public int? HomeTeamScore { get; set; }

        [JsonProperty("team_a_score")]
        public int? AwayTeamScore { get; set; }

        [JsonProperty("finished")]
        public bool Finished { get; set; }

        [JsonProperty("minutes")]
        public int Minutes { get; set; }

        [JsonProperty("provisional_start_time")]
        public bool ProvisionalStartTime { get; set; }

        [JsonProperty("finished_provisional")]
        public bool FinishedProvisional { get; set; }

        [JsonProperty("event")]
        public int? Event { get; set; }

        [JsonProperty("team_a")]
        public int AwayTeamId { get; set; }

        [JsonProperty("team_h")]
        public int HomeTeamId { get; set; }
    }
}
