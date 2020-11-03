namespace Slackbot.Net.Extensions.FplBot.Models
{
    public class PriceChange
    {
        public string PlayerFirstName { get; set; }
        public string PlayerSecondName { get; set; }
        public string PlayerWebName { get; set; }
        public int CostChangeEvent { get; set; }
        public int NowCost { get; set; }
        public string TeamName { get; set; }
    }
}