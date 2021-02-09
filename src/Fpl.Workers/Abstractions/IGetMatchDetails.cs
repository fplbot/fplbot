using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    internal interface IGetMatchDetails
    {
        Task<MatchDetails> GetMatchDetails(int pulseId);
    }
}