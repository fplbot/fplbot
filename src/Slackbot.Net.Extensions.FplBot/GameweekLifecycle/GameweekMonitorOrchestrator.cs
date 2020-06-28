using System;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.RecurringActions;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public class GameweekMonitorOrchestrator : IGameweekMonitorOrchestrator
    {
        public event Func<int, Task> InitializeEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameWeekJustBeganEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameweekIsCurrentlyOngoingEventHandlers = i => Task.CompletedTask;

        public GameweekMonitorOrchestrator(IHandleGameweekStarted startedNotifier, IMonitorFixtureEvents fixtureEventsMonitor)
        {
            InitializeEventHandlers += fixtureEventsMonitor.Initialize;
            
            GameWeekJustBeganEventHandlers += startedNotifier.HandleGameweekStarted;
            GameWeekJustBeganEventHandlers += fixtureEventsMonitor.HandleGameweekStarted;
            
            GameweekIsCurrentlyOngoingEventHandlers += fixtureEventsMonitor.HandleGameweekOngoing;
        }
        
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
    }
}