using System;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public record IndexQuery (
        DateTime TimeStamp,
        string Query,
        int Page,
        string QueriedIndex,
        string BoostedCountry,
        long TotalHits,
        long ResponseTimeMs,
        string Client,
        string Team,
        string FollowingFplLeagueId,
        string Actor) : ICommand;
}