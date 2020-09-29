using System.Collections.Generic;
using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.PriceMonitoring
{
    public class PriceChanged
    {
        public PriceChanged(ICollection<Player> players, ICollection<Team> teams)
        {
            Players = players;
            Teams = teams;
        }

        public IEnumerable<Player> Players { get; }
        public ICollection<Team> Teams { get; }
    }
}