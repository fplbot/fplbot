using System;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record TwentyFourHoursToDeadline(GameweekNearingDeadline GameweekNearingDeadline) : IEvent;

    public record OneHourToDeadline(GameweekNearingDeadline GameweekNearingDeadline) : IEvent;

    public record GameweekNearingDeadline(int Id, string Name, DateTime Deadline);
}
