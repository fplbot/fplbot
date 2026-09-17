using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace FplBot.EventHandlers;

public static class NewLeagueEntriesLookup
{
    private const long OverallPhaseId = 1;

    public record LeagueEntries(string LeagueName, IReadOnlyList<NewLeagueEntry> Entries, bool HasMore);

    public static async Task<LeagueEntries?> Fetch(
        ILeagueClient leagueClient,
        IGlobalSettingsClient globalSettingsClient,
        int leagueId,
        int gameweekId,
        ILogger logger)
    {
        var settings = await globalSettingsClient.GetGlobalSettings();

        if (PreviousDeadline(settings, gameweekId) is not { } joinedAfter)
        {
            logger.LogWarning("No deadline for gameweek {PreviousGameweekId}, skipping new entries for league {LeagueId}", gameweekId - 1, leagueId);
            return null;
        }

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
        var all = (newEntries?.Entries ?? []).ToList();
        var entries = all.Where(e => ToUtc(e.JoinedAt) > joinedAfter).ToList();

        logger.LogInformation(
            "new_entries league {LeagueId} gw{GameweekId}: fpl returned {TotalCount} (earliest {Earliest}, latest {Latest}, hasNext {HasNext}); {WindowCount} joined after the gw{PreviousGameweekId} deadline {WindowStart:o}",
            leagueId,
            gameweekId,
            all.Count,
            all.Count == 0 ? "-" : all.Min(e => ToUtc(e.JoinedAt)).ToString("o"),
            all.Count == 0 ? "-" : all.Max(e => ToUtc(e.JoinedAt)).ToString("o"),
            newEntries?.HasNext ?? false,
            entries.Count,
            gameweekId - 1,
            joinedAfter);

        if (entries.Count == 0)
        {
            return null;
        }

        return new LeagueEntries(
            league.Properties?.Name ?? $"league {leagueId}",
            entries,
            newEntries?.HasNext ?? false);
    }

    public static DateTime? PreviousDeadline(GlobalSettings? settings, int gameweekId)
    {
        var previous = settings?.Gameweeks.FirstOrDefault(g => g.Id == gameweekId - 1);
        return previous is null ? null : ToUtc(previous.Deadline);
    }

    public static int? ToPhase(GlobalSettings? settings, int gameweekId)
    {
        var phase = settings?.Phases.FirstOrDefault(p =>
            p.Id != OverallPhaseId && gameweekId >= p.StartEvent && gameweekId <= p.StopEvent);

        return phase is null ? null : (int)phase.Id;
    }

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
        : value.ToUniversalTime();
}
