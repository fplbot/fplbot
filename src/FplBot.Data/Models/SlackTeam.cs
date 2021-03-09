using System.Collections.Generic;
using Fpl.Data.Repositories;

namespace Fpl.Data.Models
{
    public class SlackTeam
    {
        public SlackTeam()
        {
            Subscriptions = new List<EventSubscription>();
        }

        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public string Scope { get; set; }
        public string AccessToken { get; set; }
        public string FplBotSlackChannel { get; set; }
        public long FplbotLeagueId { get; set; }

        /// <summary>
        /// WIP
        /// </summary>
        public IEnumerable<EventSubscription> Subscriptions { get; set; }
    }
}
