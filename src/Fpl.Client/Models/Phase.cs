using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Phase
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("start_event")]
        public long StartEvent { get; set; }

        [JsonProperty("stop_event")]
        public long StopEvent { get; set; }
    }
}
