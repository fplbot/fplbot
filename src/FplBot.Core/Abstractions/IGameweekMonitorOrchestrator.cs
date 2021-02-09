using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    public interface IGameweekMonitorOrchestrator
    {
        Task Initialize(int gameweek);
        Task GameweekJustBegan(int gameweek);
        Task GameweekIsCurrentlyOngoing(int gameweek);
        Task GameweekJustEnded(int gameweek);
        Task GameweekIsCurrentlyFinished(int gameweek);
    }
}