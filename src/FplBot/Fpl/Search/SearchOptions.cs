namespace Fpl.Search;

public class SearchOptions
{
    public required string IndexUri { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string EntriesIndex { get; set; }
    public required string LeaguesIndex { get; set; }
    public required string AnalyticsIndex { get; set; }
    public bool ShouldIndexEntries { get; set; }
    public bool ShouldIndexLeagues { get; set; }
    public int ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob { get; set; }
    public bool ResetIndexingBookmarkWhenDone { get; set; }
    public required string IndexingCron { get; set; }
}
