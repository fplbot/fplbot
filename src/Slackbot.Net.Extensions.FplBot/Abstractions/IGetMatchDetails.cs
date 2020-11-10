using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IGetMatchDetails
    {
        Task<MatchDetails> GetMatchDetails(int pulseId);
    }
}