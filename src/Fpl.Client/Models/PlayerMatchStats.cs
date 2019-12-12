using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class PlayerMatchStats
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("kickoff_time")]
        public DateTime KickoffTime { get; set; }

        [JsonProperty("kickoff_time_formatted")]
        public string FormattedKickoffTime { get; set; }

        [JsonProperty("team_h_score")]
        public int? HomeTeamScore { get; set; }

        [JsonProperty("team_a_score")]
        public int? AwayTeamScore { get; set; }

        [JsonProperty("was_home")]
        public bool WasHome { get; set; }

        [JsonProperty("round")]
        public int Round { get; set; }

        [JsonProperty("total_points")]
        public int TotalPoints { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("transfers_balance")]
        public int TransfersBalance { get; set; }

        [JsonProperty("selected")]
        public int Selected { get; set; }

        [JsonProperty("transfers_in")]
        public int TransfersIn { get; set; }

        [JsonProperty("transfers_out")]
        public int TransfersOut { get; set; }

        [JsonProperty("loaned_in")]
        public int LoanedIn { get; set; }

        [JsonProperty("loaned_out")]
        public int LoanedOut { get; set; }

        [JsonProperty("minutes")]
        public int Minutes { get; set; }

        [JsonProperty("goals_scored")]
        public int GoalsScored { get; set; }

        [JsonProperty("assists")]
        public int Assists { get; set; }

        [JsonProperty("clean_sheets")]
        public int CleanSheets { get; set; }

        [JsonProperty("goals_conceded")]
        public int GoalsConceded { get; set; }

        [JsonProperty("own_goals")]
        public int OwnGoals { get; set; }

        [JsonProperty("penalties_saved")]
        public int PenaltiesSaved { get; set; }

        [JsonProperty("penalties_missed")]
        public int PenaltiesMissed { get; set; }

        [JsonProperty("yellow_cards")]
        public int YellowCards { get; set; }

        [JsonProperty("red_cards")]
        public int RedCards { get; set; }

        [JsonProperty("saves")]
        public int Saves { get; set; }

        [JsonProperty("bonus")]
        public int Bonus { get; set; }

        [JsonProperty("bps")]
        public int Bps { get; set; }

        [JsonProperty("influence")]
        public double Influence { get; set; }

        [JsonProperty("creativity")]
        public double Creativity { get; set; }

        [JsonProperty("threat")]
        public double Threat { get; set; }

        [JsonProperty("ict_index")]
        public double IctIndex { get; set; }

        [JsonProperty("ea_index")]
        public double EaIndex { get; set; }

        [JsonProperty("open_play_crosses")]
        public int OpenPlayCrosses { get; set; }

        [JsonProperty("big_chances_created")]
        public int BigChancesCreated { get; set; }

        [JsonProperty("clearances_blocks_interceptions")]
        public int ClearancesBlocksInterceptions { get; set; }

        [JsonProperty("recoveries")]
        public int Recoveries { get; set; }

        [JsonProperty("key_passes")]
        public int KeyPasses { get; set; }

        [JsonProperty("tackles")]
        public int Tackles { get; set; }

        [JsonProperty("winning_goals")]
        public int WinningGoals { get; set; }

        [JsonProperty("attempted_passes")]
        public int AttemptedPasses { get; set; }

        [JsonProperty("completed_passes")]
        public int CompletedPasses { get; set; }

        [JsonProperty("penalties_conceded")]
        public int PenaltiesConceded { get; set; }

        [JsonProperty("big_chances_missed")]
        public int BigChancesMissed { get; set; }

        [JsonProperty("errors_leading_to_goal")]
        public int ErrorsLeadingToGoal { get; set; }

        [JsonProperty("errors_leading_to_goal_attempt")]
        public int ErrorsLeadingToGoalAttempt { get; set; }

        [JsonProperty("tackled")]
        public int Tackled { get; set; }

        [JsonProperty("offside")]
        public int Offside { get; set; }

        [JsonProperty("target_missed")]
        public int TargetMissed { get; set; }

        [JsonProperty("fouls")]
        public int Fouls { get; set; }

        [JsonProperty("dribbles")]
        public int Dribbles { get; set; }

        [JsonProperty("element")]
        public int Element { get; set; }

        [JsonProperty("fixture")]
        public int Fixture { get; set; }

        [JsonProperty("opponent_team")]
        public int OpponentTeam { get; set; }
    }
}
