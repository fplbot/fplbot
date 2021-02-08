using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IMatchStateMonitor
    {
        Task Initialize(int gw);
        Task HandleGameweekStarted(int gw);
        
        Task HandleGameweekOngoing(int gw);
        Task HandleGameweekCurrentlyFinished(int gw);
    }
}