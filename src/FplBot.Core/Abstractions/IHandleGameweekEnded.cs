using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    public interface IHandleGameweekEnded
    {
        Task HandleGameweekEnded(int gameweek);
    }
}