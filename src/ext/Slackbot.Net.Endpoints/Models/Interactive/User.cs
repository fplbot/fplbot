using Newtonsoft.Json;

namespace Slackbot.Net.Endpoints.Models.Interactive
{
    public class User
    {
        [JsonProperty("user_id")]
        public string User_Id { get; set; }
    }
}