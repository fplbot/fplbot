using System.Collections.Generic;
using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.Models
{
    public class Lineups
    {
        public int FixturePulseId { get; set; }
        public string HomeTeamNameAbbr { get; set; }
        public string AwayTeamNameAbbr { get; set; }

        public IEnumerable<PlayerInLineup>  HomeTeamLineup { get; set; }
        public IEnumerable<PlayerInLineup>  AwayTeamLineup { get; set; }
    }
}