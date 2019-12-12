using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class HeadToHeadLeagueStandings
    {
        [JsonProperty("has_next")]
        public bool HasNext { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("results")]
        public ICollection<HeadToHeadLeagueEntry> Entries { get; set; }
    }
}
