namespace FplBot.Formatting.Helpers;

public interface ICaptainsByGameWeek
{
    Task<string> GetCaptainsByGameWeek(int gameweek, IEnumerable<EntryCaptainPick> entryCaptainPicks, bool includeExternalLinks = true);
    Task<string> GetCaptainsChartByGameWeek(int gameweek, IEnumerable<EntryCaptainPick> entryCaptainPicks);
    Task<string> GetCaptainsStatsByGameWeek(IEnumerable<EntryCaptainPick> entryCaptainPicks, bool includeHeader = true);
    Task<IEnumerable<EntryCaptainPick>> GetEntryCaptainPicks(int gameweek, int leagueId);
}
