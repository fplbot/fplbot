using System.Threading.Tasks;

namespace FplBot.Slack.Abstractions
{
    internal interface ICaptainsByGameWeek
    {
        Task<string> GetCaptainsByGameWeek(int gameweek, int leagueId);
        Task<string> GetCaptainsChartByGameWeek(int gameweek, int leagueId);
    }
}
