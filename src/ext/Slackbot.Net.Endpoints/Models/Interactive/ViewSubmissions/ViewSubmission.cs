using System.Text.Json;

namespace Slackbot.Net.Endpoints.Models.Interactive.ViewSubmissions
{
    public class ViewSubmission : Interaction
    {
        public Team Team { get; set; }
        public User User { get; set; }
        
        public string ViewId { get; set; }
        public JsonElement ViewState { get; set; }
    }
}