using System.Collections.Generic;

namespace Slackbot.Net.Extensions.FplBot
{
    public class FixtureEvents
    {
        public GameScore gameScore { get; set; }

        public IDictionary<StatType, List<PlayerEvent>> StatMap { get; set; }
    }
}
