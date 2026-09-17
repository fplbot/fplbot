using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace FplBot.EventHandlers;

public static class NewLeagueEntriesLookup
{
    public record LeagueEntries(string LeagueName, IReadOnlyList<NewLeagueEntry> Entries, bool HasMore);

    public static async Task<LeagueEntries?> Fetch(
        ILeagueClient leagueClient,
        IGlobalSettingsClient globalSettingsClient,
        int leagueId,
        int gameweekId,
        ILogger logger)
    {
        var settings = await globalSettingsClient.GetGlobalSettings();
        var league = await leagueClient.GetClassicLeague(leagueId, tolerate404: true, phase: ToPhase(settings, gameweekId));
        if (league is null)
        {
            return null;
        }

        if (gameweekId <= (league.Properties?.StartEvent ?? 1))
        {
            logger.LogInformation("Skipping new entries for league {LeagueId}: gameweek {GameweekId} is the league's first", leagueId, gameweekId);
            return null;
        }

        var newEntries = league.NewEntries;
        var entries = (newEntries?.Entries ?? []).ToList();
        if (entries.Count == 0)
        {
            return null;
        }

        return new LeagueEntries(
            league.Properties?.Name ?? $"league {leagueId}",
            entries,
            newEntries?.HasNext ?? false);
    }

    public static int? ToPhase(GlobalSettings? settings, int gameweekId)
    {
        var phase = settings?.Phases.FirstOrDefault(p =>
            p.Id != OverallPhaseId && gameweekId >= p.StartEvent && gameweekId <= p.StopEvent);

        return phase is null ? null : (int)phase.Id;
    }

    private const long OverallPhaseId = 1;
}
