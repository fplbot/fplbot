using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class FixtureStatValue
    {
        [JsonProperty("element")]
        public int Element { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }
    }
}
