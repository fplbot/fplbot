using Fpl.Client.Clients;

namespace Slackbot.Net.Extensions.FplBot
{
    public class FplbotOptions : FplApiClientOptions
    {
        public int LeagueId { get; set; } = 579157; // Default: Blank-liga
        public string Channel { get; set; } = "#fplbot";
    }
}