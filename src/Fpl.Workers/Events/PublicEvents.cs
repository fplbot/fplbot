using System;
using MediatR;

namespace FplBot.Core.Models
{
    // Public events using in-mem MediatR handling:

    public record FixtureEventsOccured(FixtureUpdates FixtureEvents) : INotification;
    public record BonusAdded(int Event, DateTime MatchDayDate) : INotification;
    public record PointsReady(int Event, DateTime MatchDayDate) : INotification;
    public record LeagueStatusChanged(string prevState, string newState) : INotification;
}
