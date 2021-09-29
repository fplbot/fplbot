using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class Player
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("photo")]
        public string Photo { get; set; }

        [JsonPropertyName("web_name")]
        public string WebName { get; set; }

        [JsonPropertyName("team_code")]
        public int TeamCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("second_name")]
        public string SecondName { get; set; }

        [JsonPropertyName("squad_number")]
        public int? SquadNumber { get; set; }

        [JsonPropertyName("news")]
        public string News { get; set; }

        [JsonPropertyName("now_cost")]
        public int NowCost { get; set; }

        [JsonPropertyName("chance_of_playing_this_round")]
        public int? ChanceOfPlayingThisRound { get; set; }

        [JsonPropertyName("chance_of_playing_next_round")]
        public int? ChanceOfPlayingNextRound { get; set; }

        [JsonPropertyName("value_form")]
        public double ValueForm { get; set; }

        [JsonPropertyName("value_season")]
        public double ValueSeason { get; set; }

        [JsonPropertyName("cost_change_start")]
        public int CostChangeStart { get; set; }

        [JsonPropertyName("cost_change_event")]
        public int CostChangeEvent { get; set; }

        [JsonPropertyName("cost_change_start_fall")]
        public int CostChangeStartFall { get; set; }

        [JsonPropertyName("cost_change_event_fall")]
        public int CostChangeEventFall { get; set; }

        [JsonPropertyName("in_dreamteam")]
        public bool InDreamteam { get; set; }

        [JsonPropertyName("dreamteam_count")]
        public int DreamteamCount { get; set; }

        [JsonPropertyName("selected_by_percent")]
        public double OwnershipPercentage { get; set; }

        [JsonPropertyName("form")]
        public double Form { get; set; }

        [JsonPropertyName("transfers_out")]
        public int TransfersOut { get; set; }

        [JsonPropertyName("transfers_in")]
        public int TransfersIn { get; set; }

        [JsonPropertyName("transfers_out_event")]
        public int TransfersOutEvent { get; set; }

        [JsonPropertyName("transfers_in_event")]
        public int TransfersInEvent { get; set; }

        [JsonPropertyName("loans_in")]
        public int LoansIn { get; set; }

        [JsonPropertyName("loans_out")]
        public int LoansOut { get; set; }

        [JsonPropertyName("loaned_in")]
        public int LoanedIn { get; set; }

        [JsonPropertyName("loaned_out")]
        public int LoanedOut { get; set; }

        [JsonPropertyName("total_points")]
        public int TotalPoints { get; set; }

        [JsonPropertyName("event_points")]
        public int EventPoints { get; set; }

        [JsonPropertyName("points_per_game")]
        public double PointsPerGame { get; set; }

        [JsonPropertyName("ep_this")]
        public double? EpThis { get; set; }

        [JsonPropertyName("ep_next")]
        public double? EpNext { get; set; }

        [JsonPropertyName("special")]
        public bool Special { get; set; }

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

        [JsonPropertyName("element_type")]
        public FplPlayerPosition Position { get; set; }

        [JsonPropertyName("team")]
        public int TeamId { get; set; }

        public string FullName => $"{FirstName} {SecondName}";
    }

    public static class PlayerStatuses
    {
        public const string Available = "a";
        public const string Unavailable = "u";
        public const string Injured = "i";
        public const string NotInSquad = "n";
        public const string Suspended = "s";
        public const string Doubtful = "d";
    }
}
