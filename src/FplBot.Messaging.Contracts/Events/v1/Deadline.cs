using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

[TimeToBeReceived("00:30:00")] // discard events not being handled within 30 mins
public record TwentyFourHoursToDeadline(GameweekNearingDeadline GameweekNearingDeadline) : IEvent;

[TimeToBeReceived("00:30:00")] // discard events not being handled within 30 mins
public record OneHourToDeadline(GameweekNearingDeadline GameweekNearingDeadline) : IEvent;

public record GameweekNearingDeadline(int Id, string Name, DateTime Deadline);
