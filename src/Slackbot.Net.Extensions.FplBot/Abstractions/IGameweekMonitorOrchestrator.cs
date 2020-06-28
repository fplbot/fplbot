using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IGameweekMonitorOrchestrator
    {
        Task Initialize(int gameweek);
        Task GameweekJustBegan(int gameweek);
        Task GameweekIsCurrentlyOngoing(int gameweek);
        Task GameweekJustEnded(int gameweek);
    }
}