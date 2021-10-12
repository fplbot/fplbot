using System.Collections.Generic;
using System.Threading.Tasks;

namespace FplBot.Formatting.Helpers
{
    public interface ILeagueEntriesByGameweek
    {
        Task<IEnumerable<GameweekEntry>> GetEntriesForGameweek(int gw, int leagueId);
    }
}
