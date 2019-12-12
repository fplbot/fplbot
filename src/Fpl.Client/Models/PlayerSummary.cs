using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class PlayerSummary
    {
        [JsonProperty("history_past")]
        public ICollection<PlayerSeasonHistory> SeasonHistory { get; set; }

        [JsonProperty("fixtures")]
        public ICollection<PlayerFixture> Fixtures { get; set; }

        [JsonProperty("history")]
        public ICollection<PlayerMatchStats> MatchStats { get; set; }
    }
}
