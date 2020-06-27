using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface IState
    {
        Task Reset(int newGameweek);
        
        IEnumerable<long> GetLeagues();
        Task<IEnumerable<FixtureEvents>> Refresh(int gameweek);
        GameweekLeagueContext GetGameweekLeagueContext(long league);
    }
}    