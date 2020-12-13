using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record AppInstalled(string TeamId, string TeamName, int LeagueId, string Channel) : IEvent;
}