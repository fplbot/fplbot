using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using Xunit;

namespace FplBot.Tests
{
    public class StateTests
    {
        [Fact]
        public async Task DoesNotCrashWithNoDataReturned()
        {
            var state = CreateAllMockState();
            state.OnNewFixtureEvents += (ctx, events) =>
            {
                Assert.False(true); // should not emit this event on reset
                return Task.CompletedTask;
            };
            state.OnPriceChanges += (ctx, events) =>
            {
                Assert.False(true); // should not emit this event on reset
                return Task.CompletedTask;
            };
            await state.Reset(1);
        }

        [Fact]
        public async Task WithGoalScoredEvent()
        {
            var state = CreateGoalScoredScenario();
            var newFixtureEventsHappened = false;
            state.OnNewFixtureEvents += (context,newEvents) =>
            {
                newFixtureEventsHappened = true;
                Assert.NotEmpty(newEvents);
                Assert.Single(newEvents);
                var goalEvent = newEvents.First().StatMap[StatType.GoalsScored].First();
                Assert.Equal(PlayerEvent.TeamType.Away, goalEvent.Team);
                Assert.Equal(TestBuilder.PlayerId, goalEvent.PlayerId);
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.True(newFixtureEventsHappened);
        }
        
        [Fact]
        public async Task WithPriceIncrease()
        {
            var state = CreatePriceIncreaseScenario();
            var priceChangeEventEmitted = false;
            state.OnPriceChanges += (context,newPrices) =>
            {
                priceChangeEventEmitted = true;
                Assert.Single(newPrices);
                var priceInc = newPrices.First();
                Assert.Equal(1, priceInc.CostChangeEvent);
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.True(priceChangeEventEmitted);
        }

        private static IState CreateAllMockState()
        {
            return new State(A.Fake<IFixtureClient>(),
                A.Fake<IPlayerClient>(),
                A.Fake<ITeamsClient>(),
                A.Fake<ISlackTeamRepository>(),
                A.Fake<ITransfersByGameWeek>(),
                A.Fake<ILeagueEntriesByGameweek>(),
                A.Fake<ISlackClientBuilder>(),
                FakeLogger());
        }
        
        private static IState CreateGoalScoredScenario(string entryName = null, string slackUserRealName = null, string slackUserHandle = null)
        {
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(new List<Player>
            {
                TestBuilder.Player()
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
            
            return CreateBaseScenario(entryName, slackUserRealName, slackUserHandle, fixtureClient, playerClient);
        }
        
        private static IState CreatePriceIncreaseScenario(string entryName = null, string slackUserRealName = null, string slackUserHandle = null)
        {
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(new List<Player>
            {
                TestBuilder.Player()
            }).Once().Then.Returns(new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(1)
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
            
            return CreateBaseScenario(entryName, slackUserRealName, slackUserHandle, fixtureClient, playerClient);
        }

        private static IState CreateBaseScenario(string entryName, string slackUserRealName, string slackUserHandle, IFixtureClient fixtureClient, IPlayerClient playerClient)
        {
            var slackTeamRepository = A.Fake<ISlackTeamRepository>();
            A.CallTo(() => slackTeamRepository.GetAllTeams()).Returns(new List<SlackTeam>
            {
                TestBuilder.SlackTeam()
            });

            var transfersByGameWeek = A.Fake<ITransfersByGameWeek>();
            A.CallTo(() => transfersByGameWeek.GetTransfersByGameweek(1, 111)).Returns(new List<TransfersByGameWeek.Transfer>
            {
                new TransfersByGameWeek.Transfer
                {
                    EntryId = 2,
                    EntryName = entryName,
                    PlayerTransferredOut = TestBuilder.PlayerId
                }
            });

            var teamsClient = A.Fake<ITeamsClient>();
            A.CallTo(() => teamsClient.GetAllTeams()).Returns(new List<Team>
            {
                TestBuilder.HomeTeam(),
                TestBuilder.AwayTeam()
            });


            var slackClientService = A.Fake<ISlackClientBuilder>();

            var fakeSlackClient = A.Fake<ISlackClient>();
            A.CallTo(() => fakeSlackClient.UsersList()).Returns(new UsersListResponse
            {
                Ok = true,
                Members = new[]
                {
                    new User
                    {
                        Real_name = slackUserRealName,
                        Name = slackUserHandle
                    }
                }
            });
            A.CallTo(() => slackClientService.Build(A<string>._)).Returns(fakeSlackClient);

            return new State(fixtureClient,
                playerClient,
                teamsClient,
                slackTeamRepository,
                transfersByGameWeek,
                A.Fake<ILeagueEntriesByGameweek>(),
                slackClientService,
                FakeLogger());
        }

        private static ILogger<State> FakeLogger()
        {
            return new Logger<State>(A.Fake<ILoggerFactory>());
        }
    }
}