using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IMonitorFixtureEvents
    {
        Task Initialize(int gwId);
        Task HandleGameweekStarted(int gwId);
        Task HandleGameweekOngoing(int gwId);
        Task HandleGameweekCurrentlyFinished(int gwId);
    }
}