namespace Slackbot.Net.SlackClients.Http.Models.Responses.UserProfile
{
    public class UserProfileResponse : Response
    {
        public GetUserProfile Profile { get; set; }
    }

    public class GetUserProfile
    {
        public string Title{ get; set; }
        
        public string First_Name{ get; set; }
        public string Last_Name{ get; set; }
        
        public string Real_Name{ get; set; }
        public string Real_Name_Normalized{ get; set; }
        
        public string Phone{ get; set; }
        public string Email{ get; set; }
        public string Skype{ get; set; }
        
        public string Status_Emoji{ get; set; }
        public string Status_Text{ get; set; }
        
        public string Api_App_Id { get; set; }
        public string Bot_Id { get; set; }

    }
}