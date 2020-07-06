using System.Collections.Generic;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekLeagueContext
    {
        public IEnumerable<TransfersByGameWeek.Transfer> TransfersForLeague { get; set; }
        public ICollection<Player> Players { get; set; }
        public ICollection<Team> Teams { get; set; }
        public IEnumerable<User> Users { get; set; }
        public int LeagueId { get; set; }
        
        public int TeamId { get; set; }
    }
}