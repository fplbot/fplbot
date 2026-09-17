namespace FplBot.Data.Leagues;

/// <summary>
///     Keeps track of the most recent entry join time seen per classic league, so that polling for
///     new entries can tell new joiners from the ones already notified about.
/// </summary>
public interface ILeagueEntriesBookmarkProvider
{
    Task<DateTime?> GetLastSeenJoinTime(int leagueId);

    Task SetLastSeenJoinTime(int leagueId, DateTime joinTime);
}
