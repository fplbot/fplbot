using System.Threading.Tasks;

namespace FplBot.Formatting.Helpers;

public interface ICaptainsByGameWeek
{
    Task<string> GetCaptainsByGameWeek(int gameweek, int leagueId, bool includeExternalLinks = true);
    Task<string> GetCaptainsChartByGameWeek(int gameweek, int leagueId);
}