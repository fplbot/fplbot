using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class GameSettings
    {
        [JsonPropertyName("game")]
        public Game Game { get; set; }
    }
}
