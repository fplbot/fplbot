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

    public static readonly FplEvent[] SupportedOnWeb =
        [..Enum.GetValues<FplEvent>().Where(e => e is not FplEvent.All and not FplEvent.Taunts)];
}
