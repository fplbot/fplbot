using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public class AppInstalled  : IEvent
    {
        public AppInstalled(string TeamId, string TeamName, int LeagueId, string Channel)
        {
            this.TeamId = TeamId;
            this.TeamName = TeamName;
            this.LeagueId = LeagueId;
            this.Channel = Channel;
        }

        public string TeamId { get; }

        public string TeamName { get; }

        public int LeagueId { get; }

        public string Channel { get; }
    }
}