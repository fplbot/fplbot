using Fpl.Client.Clients;

namespace Slackbot.Net.Extensions.FplBot
{
    public class FplbotOptions : FplApiClientOptions
    {
        public int LeagueId { get; set; } = 15673; // Default: Blank-liga
        public string Channel { get; set; } = "#fplbot";
    }
}