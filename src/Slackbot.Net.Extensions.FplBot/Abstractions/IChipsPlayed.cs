using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface IChipsPlayed
    {
        Task<bool> GetHasUsedTrippleCaptainForGameWeek(int gameweek, int teamCode);
    }
}