using System;
using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class PlayerMatchStats
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("kickoff_time")]
        public DateTime KickoffTime { get; set; }

        [JsonPropertyName("kickoff_time_formatted")]
        public string FormattedKickoffTime { get; set; }

        [JsonPropertyName("team_h_score")]
        public int? HomeTeamScore { get; set; }

        [JsonPropertyName("team_a_score")]
        public int? AwayTeamScore { get; set; }

        [JsonPropertyName("was_home")]
        public bool WasHome { get; set; }

        [JsonPropertyName("round")]
        public int Round { get; set; }

        [JsonPropertyName("total_points")]
        public int TotalPoints { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("transfers_balance")]
        public int TransfersBalance { get; set; }

        [JsonPropertyName("selected")]
        public int Selected { get; set; }

        [JsonPropertyName("transfers_in")]
        public int TransfersIn { get; set; }

        [JsonPropertyName("transfers_out")]
        public int TransfersOut { get; set; }

        [JsonPropertyName("loaned_in")]
        public int LoanedIn { get; set; }

        [JsonPropertyName("loaned_out")]
        public int LoanedOut { get; set; }

        [JsonPropertyName("minutes")]
        public int Minutes { get; set; }

        [JsonPropertyName("goals_scored")]
        public int GoalsScored { get; set; }

        [JsonPropertyName("assists")]
        public int Assists { get; set; }

        [JsonPropertyName("clean_sheets")]
        public int CleanSheets { get; set; }

        [JsonPropertyName("goals_conceded")]
        public int GoalsConceded { get; set; }

        [JsonPropertyName("own_goals")]
        public int OwnGoals { get; set; }

        [JsonPropertyName("penalties_saved")]
        public int PenaltiesSaved { get; set; }

        [JsonPropertyName("penalties_missed")]
        public int PenaltiesMissed { get; set; }

        [JsonPropertyName("yellow_cards")]
        public int YellowCards { get; set; }

        [JsonPropertyName("red_cards")]
        public int RedCards { get; set; }

        [JsonPropertyName("saves")]
        public int Saves { get; set; }

        [JsonPropertyName("bonus")]
        public int Bonus { get; set; }

        [JsonPropertyName("bps")]
        public int Bps { get; set; }

        [JsonPropertyName("influence")]
        public double Influence { get; set; }

        [JsonPropertyName("creativity")]
        public double Creativity { get; set; }

        [JsonPropertyName("threat")]
        public double Threat { get; set; }

        [JsonPropertyName("ict_index")]
        public double IctIndex { get; set; }

        [JsonPropertyName("ea_index")]
        public double EaIndex { get; set; }

        [JsonPropertyName("open_play_crosses")]
        public int OpenPlayCrosses { get; set; }

        [JsonPropertyName("big_chances_created")]
        public int BigChancesCreated { get; set; }

        [JsonPropertyName("clearances_blocks_interceptions")]
        public int ClearancesBlocksInterceptions { get; set; }

        [JsonPropertyName("recoveries")]
        public int Recoveries { get; set; }

        [JsonPropertyName("key_passes")]
        public int KeyPasses { get; set; }

        [JsonPropertyName("tackles")]
        public int Tackles { get; set; }

        [JsonPropertyName("winning_goals")]
        public int WinningGoals { get; set; }

        [JsonPropertyName("attempted_passes")]
        public int AttemptedPasses { get; set; }

        [JsonPropertyName("completed_passes")]
        public int CompletedPasses { get; set; }

        [JsonPropertyName("penalties_conceded")]
        public int PenaltiesConceded { get; set; }

        [JsonPropertyName("big_chances_missed")]
        public int BigChancesMissed { get; set; }

        [JsonPropertyName("errors_leading_to_goal")]
        public int ErrorsLeadingToGoal { get; set; }

        [JsonPropertyName("errors_leading_to_goal_attempt")]
        public int ErrorsLeadingToGoalAttempt { get; set; }

        [JsonPropertyName("tackled")]
        public int Tackled { get; set; }

        [JsonPropertyName("offside")]
        public int Offside { get; set; }

        [JsonPropertyName("target_missed")]
        public int TargetMissed { get; set; }

        [JsonPropertyName("fouls")]
        public int Fouls { get; set; }

        [JsonPropertyName("dribbles")]
        public int Dribbles { get; set; }

        [JsonPropertyName("element")]
        public int Element { get; set; }

        [JsonPropertyName("fixture")]
        public int Fixture { get; set; }

        [JsonPropertyName("opponent_team")]
        public int OpponentTeam { get; set; }
    }
}
