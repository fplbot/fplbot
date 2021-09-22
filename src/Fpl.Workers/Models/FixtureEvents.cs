using System.Collections.Generic;
using Fpl.Client.Models;

namespace FplBot.Core.Models
{
    public class FixtureEvents
    {
        public GameScore GameScore { get; set; }

        public IDictionary<StatType, List<PlayerEvent>> StatMap { get; set; }
    }
}
