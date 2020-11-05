namespace Slackbot.Net.Extensions.FplBot.Models
{
    public class PlayerStatusUpdate
    {
        public string PlayerFirstName { get; set; }
        public string PlayerSecondName { get; set; }
        public string PlayerWebName { get; set; }
        
        public string TeamName { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}