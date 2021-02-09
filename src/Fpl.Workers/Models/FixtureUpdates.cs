using System.Collections.Generic;
using Fpl.Client.Models;

namespace FplBot.Core.Models
{
    public class FixtureUpdates
    {
        public ICollection<Team> Teams { get; set; }
        public ICollection<Player> Players { get; set; }
        public IEnumerable<FixtureEvents> Events { get; set; }
        public int CurrentGameweek { get; set; }
    }
}