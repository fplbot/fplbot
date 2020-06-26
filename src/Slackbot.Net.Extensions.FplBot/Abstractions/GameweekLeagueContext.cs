using System.Collections.Generic;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekLeagueContext
    {
        public IEnumerable<TransfersByGameWeek.Transfer> TransfersForLeague { get; set; }
        public ICollection<Player> Players { get; set; }
        public ICollection<Team> Teams { get; set; }
    }
}