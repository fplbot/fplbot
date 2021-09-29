using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class PlayerSummary
    {
        [JsonPropertyName("history_past")]
        public ICollection<PlayerSeasonHistory> SeasonHistory { get; set; }

        [JsonPropertyName("fixtures")]
        public ICollection<PlayerFixture> Fixtures { get; set; }

        [JsonPropertyName("history")]
        public ICollection<PlayerMatchStats> MatchStats { get; set; }
    }
}
