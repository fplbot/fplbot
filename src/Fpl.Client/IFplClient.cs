using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client
{
    public interface IFplClient
    {
        Task<PlayerStats> GetPlayerData(string playerId);
        Task<ScoreBoard> GetScoreBoard(string leagueId);
        Task<Bootstrap> GetBootstrap();
        Task<string> GetAllFplDataForPlayer(string name);
    }
}