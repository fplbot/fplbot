using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    public interface IMatchStateMonitor
    {
        Task Initialize(int gw);
        Task HandleGameweekStarted(int gw);
        
        Task HandleGameweekOngoing(int gw);
        Task HandleGameweekCurrentlyFinished(int gw);
    }
}