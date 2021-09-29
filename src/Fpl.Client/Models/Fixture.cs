using System;
using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class Fixture
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("kickoff_time_formatted")]
        public string FormattedKickOffTime { get; set; }

        [JsonPropertyName("started")]
        public bool? Started { get; set; }

        [JsonPropertyName("event_day")]
        public bool? EventDay { get; set; }

        [JsonPropertyName("deadline_time")]
        public DateTime? DeadlineTime { get; set; }

        [JsonPropertyName("deadline_time_formatted")]
        public string FormattedDeadlineTime { get; set; }

        [JsonPropertyName("stats")]
        public FixtureStat[] Stats { get; set; } = new FixtureStat[0];

        [JsonPropertyName("team_h_difficulty")]
        public int HomeTeamDifficulty { get; set; }

        [JsonPropertyName("team_a_difficulty")]
        public int AwayTeamDifficulty { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("kickoff_time")]
        public DateTime? KickOffTime { get; set; }

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

        [JsonPropertyName("pulse_id")]
        public int PulseId { get; set; }
    }
}
