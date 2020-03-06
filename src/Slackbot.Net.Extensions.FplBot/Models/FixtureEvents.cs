using System.Collections.Generic;

namespace Slackbot.Net.Extensions.FplBot.Models
{
    public class FixtureEvents
    {
        public GameScore GameScore { get; set; }

        public IDictionary<StatType, List<PlayerEvent>> StatMap { get; set; }
    }
}
