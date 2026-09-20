namespace FplBot.Formatting.Helpers;

public interface ICaptainsByGameWeek
{
    string GetCaptainsByGameWeek(int gameweek, IEnumerable<EntryCaptainPick> entryCaptainPicks, bool includeExternalLinks = true, bool markdown = true);
    string GetCaptainsChartByGameWeek(int gameweek, IEnumerable<EntryCaptainPick> entryCaptainPicks);
    string GetCaptainsStatsByGameWeek(IEnumerable<EntryCaptainPick> entryCaptainPicks, bool includeHeader = true);
    Task<IEnumerable<EntryCaptainPick>> GetEntryCaptainPicks(int gameweek, int leagueId);
}
