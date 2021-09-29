

using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class EntrySeasonHistory
    {
        [JsonPropertyName("season_name")]
        public string SeasonName { get; set; }

        [JsonPropertyName("total_points")]
        public int TotalPoints { get; set; }

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }
    }
}
