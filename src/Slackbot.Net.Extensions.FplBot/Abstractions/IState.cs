using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IState
    {
        Task Reset(int newGameweek);
        
        Task Refresh(int gameweek);
        event Func<GameweekLeagueContext, IEnumerable<FixtureEvents>, Task> OnNewFixtureEvents;
        event Func<GameweekLeagueContext, IEnumerable<PriceChange>, Task> OnPriceChanges;
    }
}    