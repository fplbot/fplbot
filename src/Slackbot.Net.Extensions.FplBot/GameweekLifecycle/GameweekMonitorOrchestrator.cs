using System;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.RecurringActions;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public class GameweekMonitorOrchestrator : IGameweekMonitorOrchestrator
    {
        public event Func<int, Task> InitializeEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameWeekJustBeganEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameweekIsCurrentlyOngoingEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameweekEndedEventHandlers = i => Task.CompletedTask;
        public event Func<int, Task> GameweekCurrentlyFinishedEventHandlers = i => Task.CompletedTask;

        public GameweekMonitorOrchestrator(IHandleGameweekStarted startedNotifier, IMonitorState stateMonitor, IMatchStateMonitor matchStateMonitor, IHandleGameweekEnded endedNotifier, IMessageSession session)
        {
            InitializeEventHandlers += stateMonitor.Initialize;
            InitializeEventHandlers += matchStateMonitor.Initialize;
            GameWeekJustBeganEventHandlers += stateMonitor.HandleGameweekStarted;
            GameWeekJustBeganEventHandlers += matchStateMonitor.HandleGameweekStarted;
            // GameWeekJustBeganEventHandlers += id => session.Publish(new GameweekStarted(id));
            GameweekIsCurrentlyOngoingEventHandlers += stateMonitor.HandleGameweekOngoing;
            GameweekIsCurrentlyOngoingEventHandlers += matchStateMonitor.HandleGameweekOngoing;
            // GameweekIsCurrentlyOngoingEventHandlers += id => session.Publish(new GameweekOngoing(id));
            GameweekCurrentlyFinishedEventHandlers += stateMonitor.HandleGameweekCurrentlyFinished;
            GameweekCurrentlyFinishedEventHandlers += matchStateMonitor.HandleGameweekCurrentlyFinished;
            // GameweekCurrentlyFinishedEventHandlers += id => session.Publish(new GameweekCurrentlyFinished(id));
            
            GameWeekJustBeganEventHandlers += startedNotifier.HandleGameweekStarted;
            GameweekEndedEventHandlers += endedNotifier.HandleGameweekEnded;
            // GameweekEndedEventHandlers += id => session.Publish(new GameweekEnded(id));
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