using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface ILeagueEntriesByGameweek
    {
        Task<IEnumerable<GameweekEntry>> GetEntriesForGameweek(int gw, int leagueId);
    }
}