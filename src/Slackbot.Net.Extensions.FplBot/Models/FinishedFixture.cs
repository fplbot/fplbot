using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.Models
{
    public class FinishedFixture
    {
        public Fixture Fixture { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }
    }
}