using System.Collections.Generic;
using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.PriceMonitoring
{
    public class PriceChanged
    {
        public PriceChanged(ICollection<Player> players)
        {
            Players = players;
        }

        public IEnumerable<Player> Players { get; }
    }
}