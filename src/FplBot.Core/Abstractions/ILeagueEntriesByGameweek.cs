using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Core.Helpers;

namespace FplBot.Core.Abstractions
{
    public interface ILeagueEntriesByGameweek
    {
        Task<IEnumerable<GameweekEntry>> GetEntriesForGameweek(int gw, int leagueId);
    }
}