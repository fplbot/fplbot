using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class HeadToHeadLeagueMatch
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("entry_1_entry")]
        public int TeamAId { get; set; }

        [JsonProperty("entry_1_name")]
        public string TeamAName { get; set; }

        [JsonProperty("entry_1_player_name")]
        public string TeamAPlayerName { get; set; }

        [JsonProperty("entry_2_entry")]
        public int TeamBId { get; set; }

        [JsonProperty("entry_2_name")]
        public string TeamBName { get; set; }

        [JsonProperty("entry_2_player_name")]
        public string TeamBPlayerName { get; set; }

        [JsonProperty("is_knockout")]
        public bool IsKnockout { get; set; }

        [JsonProperty("winner")]
        public string Winner { get; set; }

        [JsonProperty("tiebreak")]
        public string Tiebreak { get; set; }

        [JsonProperty("own_entry")]
        public bool OwnEntry { get; set; }

        [JsonProperty("entry_1_points")]
        public int TeamAPoints { get; set; }

        [JsonProperty("entry_1_win")]
        public int TeamAWin { get; set; }

        [JsonProperty("entry_1_draw")]
        public int TeamADraw { get; set; }

        [JsonProperty("entry_1_loss")]
        public int TeamALoss { get; set; }

        [JsonProperty("entry_2_points")]
        public int TeamBPoints { get; set; }

        [JsonProperty("entry_2_win")]
        public int TeamBWin { get; set; }

        [JsonProperty("entry_2_draw")]
        public int TeamBDraw { get; set; }

        [JsonProperty("entry_2_loss")]
        public int TeamBLoss { get; set; }

        [JsonProperty("entry_1_total")]
        public int TeamATotal { get; set; }

        [JsonProperty("entry_2_total")]
        public int TeamBTotal { get; set; }

        [JsonProperty("seed_value")]
        public string SeedValue { get; set; }

        [JsonProperty("event")]
        public int Event { get; set; }
    }
}
