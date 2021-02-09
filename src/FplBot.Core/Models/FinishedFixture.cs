using System.Collections.Generic;
using Fpl.Client.Models;

namespace FplBot.Core.Models
{
    public class FinishedFixture
    {
        public Fixture Fixture { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }

        public IEnumerable<BonusPointsPlayer> BonusPoints { get; set; } = new List<BonusPointsPlayer>();
    }

    public class BonusPointsPlayer
    {
        public Player Player { get; set; }
        public int BonusPoints { get; set; }
    }
}