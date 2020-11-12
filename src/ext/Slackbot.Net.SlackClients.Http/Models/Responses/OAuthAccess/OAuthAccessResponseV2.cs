namespace Slackbot.Net.SlackClients.Http.Models.Responses.OAuthAccess
{
    public class OAuthAccessResponseV2 : Response
    {
        public string Access_Token { get; set; }
        public string Scope { get; set; }
        public Team Team { get; set; }
        public string App_Id { get; set; }
        public OAuthUser Authed_user { get; set; }
    }

    public class Team
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}