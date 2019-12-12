using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class PickStats
    {
        [JsonProperty("yellow_cards")]
        public int YellowCards { get; set; }

        [JsonProperty("own_goals")]
        public int OwnGoals { get; set; }

        [JsonProperty("creativity")]
        public double Creativity { get; set; }

        [JsonProperty("goals_conceded")]
        public int GoalsConceded { get; set; }

        [JsonProperty("bonus")]
        public int Bonus { get; set; }

        [JsonProperty("red_cards")]
        public int RedCards { get; set; }

        [JsonProperty("saves")]
        public int Saves { get; set; }

        [JsonProperty("influence")]
        public double Influence { get; set; }

        [JsonProperty("bps")]
        public int Bps { get; set; }

        [JsonProperty("clean_sheets")]
        public int CleanSheets { get; set; }

        [JsonProperty("assists")]
        public int Assists { get; set; }

        [JsonProperty("ict_index")]
        public double IctIndex { get; set; }

        [JsonProperty("goals_scored")]
        public int GoalsScored { get; set; }

        [JsonProperty("threat")]
        public double Threat { get; set; }

        [JsonProperty("penalties_missed")]
        public int PenaltiesMissed { get; set; }

        [JsonProperty("total_points")]
        public int TotalPoints { get; set; }

        [JsonProperty("penalties_saved")]
        public int PenaltiesSaved { get; set; }

        [JsonProperty("in_dreamteam")]
        public bool InDreamteam { get; set; }

        [JsonProperty("minutes")]
        public int Minutes { get; set; }
    }
}
