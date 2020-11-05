using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IState
    {
        Task Reset(int newGameweek);
        
        Task Refresh(int gameweek);
        event Func<int,IEnumerable<FixtureEvents>, Task> OnNewFixtureEvents;
        event Func<IEnumerable<PriceChange>, Task> OnPriceChanges;
        event Func<IEnumerable<PlayerStatusUpdate>, Task> OnStatusUpdates;
    }
}    