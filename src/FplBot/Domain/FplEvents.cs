namespace FplBot.Domain;

public static class FplEvents
{
    public static readonly FplEvent[] RequiringALeague =
    [
        FplEvent.Standings,
        FplEvent.Captains,
        FplEvent.Transfers,
        FplEvent.Taunts
    ];
}
