using System.Collections.Generic;

namespace FplBot.Core.Models
{
    public class FixtureEvents
    {
        public GameScore GameScore { get; set; }

        public IDictionary<StatType, List<PlayerEvent>> StatMap { get; set; }
    }
}
