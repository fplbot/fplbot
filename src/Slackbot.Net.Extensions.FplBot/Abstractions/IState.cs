using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IState
    {
        Task Reset(int newGameweek);
        
        Task Refresh(int gameweek);
        event Func<FixtureUpdates, Task> OnNewFixtureEvents;
        event Func<IEnumerable<PriceChange>, Task> OnPriceChanges;
        event Func<IEnumerable<PlayerStatusUpdate>, Task> OnStatusUpdates;
        event Func<IEnumerable<FinishedFixture>, Task> OnFixturesProvisionalFinished;
    }
}    