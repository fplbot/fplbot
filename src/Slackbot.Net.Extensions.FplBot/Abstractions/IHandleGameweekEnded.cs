using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IHandleGameweekEnded
    {
        Task HandleGameweekEnded(int gameweek);
    }
}