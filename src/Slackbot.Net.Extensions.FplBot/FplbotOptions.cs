using System.Collections.Generic;
using Fpl.Client.Clients;

namespace Slackbot.Net.Extensions.FplBot
{
    public class FplbotOptions : FplApiClientOptions
    {
        public int LeagueId { get; set; } = 579157; // Default: Blank-liga
        public string Channel { get; set; } = "#fplbot";
        public List<PlayerNickName> NickNames { get; set; } = new List<PlayerNickName>();
    }

    public class PlayerNickName
    {
        public string NickName { get; set; }
        public string RealName { get; set; }
    }
}