using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    public interface IHandleGameweekStarted
    {
        Task HandleGameweekStarted(int newGameweek);
    }
}