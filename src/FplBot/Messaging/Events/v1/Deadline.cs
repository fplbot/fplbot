namespace FplBot.Messaging.Contracts.Events.v1;

public record TwentyFourHoursToDeadline(GameweekNearingDeadline GameweekNearingDeadline);

public record OneHourToDeadline(GameweekNearingDeadline GameweekNearingDeadline);

public record GameweekNearingDeadline(int Id, string Name, DateTime Deadline);
