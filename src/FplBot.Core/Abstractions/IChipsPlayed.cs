using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface IChipsPlayed
    {
        Task<bool> GetHasUsedTripleCaptainForGameWeek(int gameweek, int teamCode);
    }
}