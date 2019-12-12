using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class ClassicLeague
    {
        [JsonProperty("new_entries")]
        public NewLeagueEntries NewEntries { get; set; }

        [JsonProperty("league")]
        public ClassicLeagueProperties Properties { get; set; }

        [JsonProperty("standings")]
        public ClassicLeagueStandings Standings { get; set; }
    }
}
