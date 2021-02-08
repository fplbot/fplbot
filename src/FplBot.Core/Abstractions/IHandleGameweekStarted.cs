using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    public interface IHandleGameweekStarted
    {
        Task HandleGameweekStarted(int newGameweek);
    }
}