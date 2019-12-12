using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class HeadToHeadLeagueEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("entry_name")]
        public string EntryName { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }

        [JsonProperty("movement")]
        public string Movement { get; set; }

        [JsonProperty("own_entry")]
        public bool OwnEntry { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("last_rank")]
        public int LastRank { get; set; }

        [JsonProperty("rank_sort")]
        public int RankSort { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("matches_played")]
        public int MatchesPlayed { get; set; }

        [JsonProperty("matches_won")]
        public int MatchesWon { get; set; }

        [JsonProperty("matches_drawn")]
        public int MatchesDrawn { get; set; }

        [JsonProperty("matches_lost")]
        public int MatchesLost { get; set; }

        [JsonProperty("points_for")]
        public int PointsFor { get; set; }

        [JsonProperty("points_against")]
        public int PointsAgainst { get; set; }

        [JsonProperty("points_total")]
        public int PointsTotal { get; set; }

        [JsonProperty("division")]
        public int Division { get; set; }

        [JsonProperty("entry")]
        public int Entry { get; set; }
    }
}
