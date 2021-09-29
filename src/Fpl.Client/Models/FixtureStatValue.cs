using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class FixtureStatValue
    {
        [JsonPropertyName("element")]
        public int Element { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
