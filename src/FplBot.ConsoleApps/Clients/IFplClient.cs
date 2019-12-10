using System.Threading.Tasks;

namespace FplBot.ConsoleApps.Clients
{
    public interface IFplClient
    {
        Task<string> GetStandings(string leagueId);
        Task<string> GetAllFplDataForPlayer(string name);
    }
}