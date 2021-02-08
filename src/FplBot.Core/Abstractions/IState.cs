using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Core.Models;

namespace FplBot.Core.Abstractions
{
    public interface IState
    {
        Task Reset(int newGameweek);
        
        Task Refresh(int gameweek);
        event Func<FixtureUpdates, Task> OnNewFixtureEvents;
        event Func<IEnumerable<PlayerUpdate>, Task> OnPriceChanges;
        event Func<IEnumerable<PlayerUpdate>, Task> OnInjuryUpdates;
        event Func<IEnumerable<FinishedFixture>, Task> OnFixturesProvisionalFinished;
    }
}    