using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.Models
{
    public class PlayerStatusUpdate
    {
        public string PlayerFirstName { get; set; }
        public string PlayerSecondName { get; set; }
        public string PlayerWebName { get; set; }
        
        public string News { get; set; }
        public string TeamName { get; set; }
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }

        public void Deconstruct(out string fromStatus, out string toStatus)
        {
            fromStatus = FromStatus;
            toStatus = ToStatus;
        }
    }
}