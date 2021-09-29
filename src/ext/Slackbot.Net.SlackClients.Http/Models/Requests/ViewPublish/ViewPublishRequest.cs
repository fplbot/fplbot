namespace Slackbot.Net.SlackClients.Http.Models.Requests.ViewPublish
{
    public class ViewPublishRequest
    {
        public ViewPublishRequest(string userId)
        {
            User_Id = userId;
        }

        public string User_Id { get; }
        
        public View View { get; set; }
    }
}