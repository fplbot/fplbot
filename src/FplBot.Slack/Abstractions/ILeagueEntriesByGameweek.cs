using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Slack.Helpers;

namespace FplBot.Slack.Abstractions
{
    public interface ILeagueEntriesByGameweek
    {
        Task<IEnumerable<GameweekEntry>> GetEntriesForGameweek(int gw, int leagueId);
    }
}
