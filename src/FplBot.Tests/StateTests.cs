using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class StateTests
    {
        private readonly ITestOutputHelper _helper;

        public StateTests(ITestOutputHelper helper)
        {
            _helper = helper;
        }
        
        [Fact]
        public async Task DoesNotCrashWithNoDataReturned()
        {
            var state = CreateAllMockState();
            await state.Reset(1);
            var current = state.GetLeagues();
            Assert.Empty(current);
            
            var contextForLeague = state.GetGameweekLeagueContext(1337);
            Assert.Empty(contextForLeague.Players);
            Assert.Empty(contextForLeague.Teams);
            Assert.Empty(contextForLeague.TransfersForLeague);
        }

        [Fact]
        public async Task WithGoalScoredEvent()
        {
            var state = CreateGoalScoredScenario();
            await state.Reset(1);
            var newEvents = await state.Refresh(1);
            Assert.NotEmpty(newEvents);
            Assert.Single(newEvents);
            var goalEvent = newEvents.First().StatMap[StatType.GoalsScored].First();
            Assert.Equal(PlayerEvent.TeamType.Away, goalEvent.Team);
            Assert.Equal(TestBuilder.PlayerId, goalEvent.PlayerId);
            
            var context = state.GetGameweekLeagueContext(TestBuilder.LeagueId);
            
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), context);
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine(formatttedEvent);
            }
            Assert.Contains("PlayerFirstName PlayerSecondName just scored a goal", formattedEvents.First());
        }

        private static IState CreateAllMockState()
        {
            return new State(A.Fake<IFixtureClient>(),
                A.Fake<IPlayerClient>(),
                A.Fake<ITeamsClient>(),
                A.Fake<ISlackTeamRepository>(),
                A.Fake<ITransfersByGameWeek>());
        }
        
        private static IState CreateGoalScoredScenario()
        {
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(new List<Player>
            {
                TestBuilder.Player()
            });
            
            var slackTeamRepository = A.Fake<ISlackTeamRepository>();
            A.CallTo(() => slackTeamRepository.GetAllTeamsAsync()).Returns(new List<SlackTeam>
            {
                TestBuilder.SlackTeam()
            });
            
            var transfersByGameWeek = A.Fake<ITransfersByGameWeek>();
            A.CallTo(() => transfersByGameWeek.GetTransfersByGameweek(1, 111)).Returns(new List<TransfersByGameWeek.Transfer>{ new TransfersByGameWeek.Transfer { EntryId = 2 }});

            var teamsClient = A.Fake<ITeamsClient>();
            A.CallTo(() => teamsClient.GetAllTeams()).Returns(new List<Team>
            {
               TestBuilder.HomeTeam(),
               TestBuilder.AwayTeam()
            });


            var fixtureClient = A.Fake<IFixtureClient>();
            A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(888, 1)
            }).Once()
                .Then.Returns(
                new List<Fixture>
                {
                    TestBuilder.AwayTeamGoal(888, 2)
                });
            
         
            
            return new State(fixtureClient,
                playerClient,
                teamsClient,
                slackTeamRepository,
                transfersByGameWeek);
        }
    }
}