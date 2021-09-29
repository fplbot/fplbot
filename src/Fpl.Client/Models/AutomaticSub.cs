

using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class AutomaticSub
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("element_in")]
        public int ElementIn { get; set; }

        [JsonPropertyName("element_out")]
        public int ElementOut { get; set; }

        [JsonPropertyName("entry")]
        public int Entry { get; set; }

        [JsonPropertyName("event")]
        public int Event { get; set; }
    }
}
