using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client.Abstractions
{
    public interface ILeagueClient
    {
        Task<ClassicLeague> GetClassicLeague(int leagueId, int page = 1);

        Task<HeadToHeadLeague> GetHeadToHeadLeague(int leagueId, int page = 1);
    }
}