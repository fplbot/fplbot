using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IHandleGameweekEnded
    {
        Task HandleGameweekEndeded(int gameweek);
    }
}