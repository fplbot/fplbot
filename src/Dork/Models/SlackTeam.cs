namespace Dork.Models
{
    public class SlackTeam
    {
        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public string Scope { get; set; }
        public string AccessToken { get; set; }
        public string FplBotSlackChannel { get; set; }
        public long FplbotLeagueId { get; set; }
    }
}   