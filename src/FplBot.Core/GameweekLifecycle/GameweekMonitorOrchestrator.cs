using System;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;

namespace FplBot.Core.GameweekLifecycle
{
    public class GameweekMonitorOrchestrator : IGameweekMonitorOrchestrator
    {
        public event Func<int, Task> InitializeEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameWeekJustBeganEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameweekIsCurrentlyOngoingEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameweekEndedEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameweekCurrentlyFinishedEventHandlers = i => Task.CompletedTask;

        public async Task Initialize(int gameweek)
        {
            await InitializeEventHandlers(gameweek);
        }

        public async Task GameweekJustBegan(int gameweek)
        {
            await GameWeekJustBeganEventHandlers(gameweek);
        }

        public async Task GameweekIsCurrentlyOngoing(int gameweek)
        {
            await GameweekIsCurrentlyOngoingEventHandlers(gameweek);
        }

        public async Task GameweekJustEnded(int gameweek)
        {
            await GameweekEndedEventHandlers(gameweek);
        }

        public async Task GameweekIsCurrentlyFinished(int gameweek)
        {
            await GameweekCurrentlyFinishedEventHandlers(gameweek);
        }
    }
}