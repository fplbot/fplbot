using System.Threading.Tasks;
using FakeItEasy;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle;
using Slackbot.Net.Extensions.FplBot.RecurringActions;
using Xunit;

namespace FplBot.Tests
{
    public class GameweekMonitorOrchestratorTests
    {
        [Fact]
        public async Task WhenGameweekJustBegun_TriggersEvent()
        {
            var orchestrator = CreateOrchestrator();
            
            int? gwId = null;
            orchestrator.GameWeekJustBeganEventHandlers +=  i =>
            {
                gwId = i;
                return Task.CompletedTask;
            };

            await orchestrator.GameweekJustBegan(1);
            
            Assert.NotNull(gwId);
            Assert.Equal(1, gwId.Value);
        }

        [Fact]
        public async Task WhenGameweekIsOngoing_TriggersEvent()
        {
            var orchestrator = CreateOrchestrator();
            
            int? gwId = null;
            orchestrator.GameweekIsCurrentlyOngoingEventHandlers +=  i =>
            {
                gwId = i;
                return Task.CompletedTask;
            };

            await orchestrator.GameweekIsCurrentlyOngoing(1);
            
            Assert.NotNull(gwId);
            Assert.Equal(1, gwId.Value);
        }
        
        [Fact]
        public async Task WhenGameweekJustEnded_TriggersEvent()
        {
            var orchestrator = CreateOrchestrator();
            
            int? gwId = null;
            orchestrator.GameweekEndedEventHandlers +=  i =>
            {
                gwId = i;
                return Task.CompletedTask;
            };

            await orchestrator.GameweekJustEnded(1);
            
            Assert.NotNull(gwId);
            Assert.Equal(1, gwId.Value);
        }

        private static GameweekMonitorOrchestrator CreateOrchestrator()
        {
            var orchestrator = new GameweekMonitorOrchestrator(A.Fake<IHandleGameweekStarted>(), A.Fake<IMonitorState>(), A.Fake<IMatchStateMonitor>(), A.Fake<IHandleGameweekEnded>(), A.Fake<IMessageSession>());
            return orchestrator;
        }
    }
}