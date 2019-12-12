using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class GameSettings
    {
        [JsonProperty("game")]
        public Game Game { get; set; }
    }
}
