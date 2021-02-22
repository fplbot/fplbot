using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Player
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("photo")]
        public string Photo { get; set; }

        [JsonProperty("web_name")]
        public string WebName { get; set; }

        [JsonProperty("team_code")]
        public int TeamCode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("second_name")]
        public string SecondName { get; set; }

        [JsonProperty("squad_number")]
        public int? SquadNumber { get; set; }

        [JsonProperty("news")]
        public string News { get; set; }

        [JsonProperty("now_cost")]
        public int NowCost { get; set; }

        [JsonProperty("chance_of_playing_this_round")]
        public string ChanceOfPlayingThisRound { get; set; }

        [JsonProperty("chance_of_playing_next_round")]
        public string ChanceOfPlayingNextRound { get; set; }

        [JsonProperty("value_form")]
        public double ValueForm { get; set; }

        [JsonProperty("value_season")]
        public double ValueSeason { get; set; }

        [JsonProperty("cost_change_start")]
        public int CostChangeStart { get; set; }

        [JsonProperty("cost_change_event")]
        public int CostChangeEvent { get; set; }

        [JsonProperty("cost_change_start_fall")]
        public int CostChangeStartFall { get; set; }

        [JsonProperty("cost_change_event_fall")]
        public int CostChangeEventFall { get; set; }

        [JsonProperty("in_dreamteam")]
        public bool InDreamteam { get; set; }

        [JsonProperty("dreamteam_count")]
        public int DreamteamCount { get; set; }

        [JsonProperty("selected_by_percent")]
        public double OwnershipPercentage { get; set; }

        [JsonProperty("form")]
        public double Form { get; set; }

        [JsonProperty("transfers_out")]
        public int TransfersOut { get; set; }

        [JsonProperty("transfers_in")]
        public int TransfersIn { get; set; }

        [JsonProperty("transfers_out_event")]
        public int TransfersOutEvent { get; set; }

        [JsonProperty("transfers_in_event")]
        public int TransfersInEvent { get; set; }

        [JsonProperty("loans_in")]
        public int LoansIn { get; set; }

        [JsonProperty("loans_out")]
        public int LoansOut { get; set; }

        [JsonProperty("loaned_in")]
        public int LoanedIn { get; set; }

        [JsonProperty("loaned_out")]
        public int LoanedOut { get; set; }

        [JsonProperty("total_points")]
        public int TotalPoints { get; set; }

        [JsonProperty("event_points")]
        public int EventPoints { get; set; }

        [JsonProperty("points_per_game")]
        public double PointsPerGame { get; set; }

        [JsonProperty("ep_this")]
        public double? EpThis { get; set; }

        [JsonProperty("ep_next")]
        public double? EpNext { get; set; }

        [JsonProperty("special")]
        public bool Special { get; set; }

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

        [JsonProperty("element_type")]
        public FplPlayerPosition Position { get; set; }

        [JsonProperty("team")]
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
