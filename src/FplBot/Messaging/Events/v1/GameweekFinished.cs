namespace FplBot.Messaging.Contracts.Events.v1;

public record GameweekFinished(FinishedGameweek FinishedGameweek);

public record FinishedGameweek(int Id);
