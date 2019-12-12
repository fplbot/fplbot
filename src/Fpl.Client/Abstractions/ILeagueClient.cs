using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client.Abstractions
{
    public interface ILeagueClient
    {
        Task<ClassicLeague> GetClassicLeague(int leagueId);

        Task<HeadToHeadLeague> GetHeadToHeadLeague(int leagueId);
    }
}