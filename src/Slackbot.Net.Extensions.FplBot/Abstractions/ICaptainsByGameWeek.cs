using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public interface ICaptainsByGameWeek
    {
        Task<string> GetCaptainsByGameWeek(int gameweek);
    }
}