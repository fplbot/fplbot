using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class FixtureStats
    {
        [JsonPropertyName("goals_scored")]
        public FixtureStat GoalsScored { get; set; }

        [JsonPropertyName("assists")]
        public FixtureStat Assists { get; set; }

        [JsonPropertyName("own_goals")]
        public FixtureStat OwnGoals { get; set; }

        [JsonPropertyName("penalties_saved")]
        public FixtureStat PenaltiesSaved { get; set; }

        [JsonPropertyName("penalties_missed")]
        public FixtureStat PenaltiesMissed { get; set; }

        [JsonPropertyName("yellow_cards")]
        public FixtureStat YellowCards { get; set; }

        [JsonPropertyName("red_cards")]
        public FixtureStat RedCards { get; set; }

        [JsonPropertyName("saves")]
        public FixtureStat Saves { get; set; }

        [JsonPropertyName("bonus")]
        public FixtureStat Bonus { get; set; }

        [JsonPropertyName("bps")]
        public FixtureStat Bps { get; set; }
    }
}
