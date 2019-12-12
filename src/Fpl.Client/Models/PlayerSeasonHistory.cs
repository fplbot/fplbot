using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class PlayerSeasonHistory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("season_name")]
        public string SeasonName { get; set; }

        [JsonProperty("element_code")]
        public int ElementCode { get; set; }

        [JsonProperty("start_cost")]
        public int StartCost { get; set; }

        [JsonProperty("end_cost")]
        public int EndCost { get; set; }

        [JsonProperty("total_points")]
        public int TotalPoints { get; set; }

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

        [JsonProperty("season")]
        public int Season { get; set; }
    }
}
