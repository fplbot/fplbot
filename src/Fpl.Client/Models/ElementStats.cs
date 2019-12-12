using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class ElementStats
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
