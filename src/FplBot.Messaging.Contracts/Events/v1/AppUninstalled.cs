using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record AppUninstalled(string TeamId, string TeamName, int LeagueId, string Channel) : IEvent;
}