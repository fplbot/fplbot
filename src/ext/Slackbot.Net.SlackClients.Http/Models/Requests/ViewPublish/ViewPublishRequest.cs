using Newtonsoft.Json;

namespace Slackbot.Net.SlackClients.Http.Models.Requests.ViewPublish
{
    public class ViewPublishRequest
    {
        public ViewPublishRequest(string userId)
        {
            User_Id = userId;
        }
        
        [JsonProperty("user_id")]
        public string User_Id { get; }
        
        public View View { get; set; }
    }
}