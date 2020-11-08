using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.Models
{
    public class PlayerStatusUpdate
    {
        public Player FromPlayer { get; set; }
        public Player ToPlayer { get; set; }
        public string TeamName { get; set; }

        public void Deconstruct(out string fromStatus, out string toStatus)
        {
            fromStatus = FromPlayer?.Status;
            toStatus = ToPlayer?.Status;
        }
    }
}