using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    internal interface IChipsPlayed
    {
        Task<bool> GetHasUsedTripleCaptainForGameWeek(int gameweek, int teamCode);
    }
}