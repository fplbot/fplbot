using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class HeadToHeadLeagueMatch
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("entry_1_entry")]
        public int TeamAId { get; set; }

        [JsonPropertyName("entry_1_name")]
        public string TeamAName { get; set; }

        [JsonPropertyName("entry_1_player_name")]
        public string TeamAPlayerName { get; set; }

        [JsonPropertyName("entry_2_entry")]
        public int TeamBId { get; set; }

        [JsonPropertyName("entry_2_name")]
        public string TeamBName { get; set; }

        [JsonPropertyName("entry_2_player_name")]
        public string TeamBPlayerName { get; set; }

        [JsonPropertyName("is_knockout")]
        public bool IsKnockout { get; set; }

        [JsonPropertyName("winner")]
        public string Winner { get; set; }

        [JsonPropertyName("tiebreak")]
        public string Tiebreak { get; set; }

        [JsonPropertyName("own_entry")]
        public bool OwnEntry { get; set; }

        [JsonPropertyName("entry_1_points")]
        public int TeamAPoints { get; set; }

        [JsonPropertyName("entry_1_win")]
        public int TeamAWin { get; set; }

        [JsonPropertyName("entry_1_draw")]
        public int TeamADraw { get; set; }

        [JsonPropertyName("entry_1_loss")]
        public int TeamALoss { get; set; }

        [JsonPropertyName("entry_2_points")]
        public int TeamBPoints { get; set; }

        [JsonPropertyName("entry_2_win")]
        public int TeamBWin { get; set; }

        [JsonPropertyName("entry_2_draw")]
        public int TeamBDraw { get; set; }

        [JsonPropertyName("entry_2_loss")]
        public int TeamBLoss { get; set; }

        [JsonPropertyName("entry_1_total")]
        public int TeamATotal { get; set; }

        [JsonPropertyName("entry_2_total")]
        public int TeamBTotal { get; set; }

        [JsonPropertyName("seed_value")]
        public string SeedValue { get; set; }

        [JsonPropertyName("event")]
        public int Event { get; set; }
    }
}
