using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    public interface IMonitorState
    {
        Task Initialize(int gwId);
        Task HandleGameweekStarted(int gwId);
        Task HandleGameweekOngoing(int gwId);
        Task HandleGameweekCurrentlyFinished(int gwId);
    }
}