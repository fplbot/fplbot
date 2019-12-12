using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class FixtureStats
    {
        [JsonProperty("goals_scored")]
        public FixtureStat GoalsScored { get; set; }

        [JsonProperty("assists")]
        public FixtureStat Assists { get; set; }

        [JsonProperty("own_goals")]
        public FixtureStat OwnGoals { get; set; }

        [JsonProperty("penalties_saved")]
        public FixtureStat PenaltiesSaved { get; set; }

        [JsonProperty("penalties_missed")]
        public FixtureStat PenaltiesMissed { get; set; }

        [JsonProperty("yellow_cards")]
        public FixtureStat YellowCards { get; set; }

        [JsonProperty("red_cards")]
        public FixtureStat RedCards { get; set; }

        [JsonProperty("saves")]
        public FixtureStat Saves { get; set; }

        [JsonProperty("bonus")]
        public FixtureStat Bonus { get; set; }

        [JsonProperty("bps")]
        public FixtureStat Bps { get; set; }
    }
}
