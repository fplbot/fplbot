using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class FixtureStat
    {
        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("a")]
        public ICollection<FixtureStatValue> AwayStats { get; set; }

        [JsonProperty("h")]
        public ICollection<FixtureStatValue> HomeStats { get; set; }
    }
}
