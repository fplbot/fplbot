using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    public interface IGetMatchDetails
    {
        Task<MatchDetails> GetMatchDetails(int pulseId);
    }
}