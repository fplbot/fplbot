using System;
using MediatR;

namespace FplBot.Core.Models
{
    // Public events using in-mem MediatR handling:

    public record FixtureEventsOccured(FixtureUpdates FixtureEvents) : INotification;
}
