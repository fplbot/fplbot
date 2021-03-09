using System.Collections.Generic;
using Fpl.Client.Models;
using FplBot.Core.Helpers;
using FplBot.Data.Models;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace FplBot.Core.Abstractions
{
    public class GameweekLeagueContext
    {
        public IEnumerable<TransfersByGameWeek.Transfer> TransfersForLeague { get; set; }
        public IEnumerable<GameweekEntry> GameweekEntries { get; set; }
        public ICollection<Player> Players { get; set; }
        public ICollection<Team> Teams { get; set; }
        public IEnumerable<User> Users { get; set; }
        public SlackTeam SlackTeam { get; set; }
    }
}
