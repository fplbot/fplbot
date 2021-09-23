using System;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record MatchdayLeaguesUpdated() : IEvent;
}
