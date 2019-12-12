using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class HeadToHeadLeague
    {
        [JsonProperty("new_entries")]
        public NewLeagueEntries NewEntries { get; set; }

        [JsonProperty("league")]
        public HeadToHeadLeagueProperties Properties { get; set; }

        [JsonProperty("standings")]
        public HeadToHeadLeagueStandings Standings { get; set; }

        [JsonProperty("matches_next")]
        public HeadToHeadLeagueMatches Next { get; set; }

        [JsonProperty("matches_this")]
        public HeadToHeadLeagueMatches Current { get; set; }
    }
}
