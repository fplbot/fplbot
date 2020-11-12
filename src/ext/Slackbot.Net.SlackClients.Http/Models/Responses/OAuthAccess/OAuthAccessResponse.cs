namespace Slackbot.Net.SlackClients.Http.Models.Responses.OAuthAccess
{
    public class OAuthAccessResponse : Response
    {
        public string Access_Token { get; set; }
        public string Scope { get; set; }
        public string Team_Name { get; set; }
        public string Team_Id { get; set; }
        public Bot Bot { get; set; }

        public string App_Id { get; set; }
        public string App_User_Id { get; set; }
        public OAuthUser Authorizing_User { get; set; }
        public OAuthUser Installer_User { get; set; }
    }

    public class Bot
    {
        public string Bot_User_Id { get; set; }
        public string Bot_Access_Token { get; set; }
    }

    public class OAuthUser
    {
        public string User_Id { get; set; }
        public string App_Home { get; set; }
    }
}