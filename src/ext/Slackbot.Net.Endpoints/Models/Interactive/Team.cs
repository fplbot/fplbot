using Newtonsoft.Json;

namespace Slackbot.Net.Endpoints.Models.Interactive
{
    public class Team
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}