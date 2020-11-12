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
using Xunit;

namespace FplBot.Tests
{
    public class StateTests
    {
        [Fact]
        public async Task DoesNotCrashWithNoDataReturned()
        {
            var state = CreateAllMockState();
            var wasCalled = false;
            state.OnNewFixtureEvents += (events) =>
            {
                Assert.False(true); // should not emit this event on reset
                wasCalled = true;
                return Task.CompletedTask;
            };
            state.OnPriceChanges += events =>
            {
                wasCalled = true;
                Assert.False(true); // should not emit this event on reset
                return Task.CompletedTask;
            };
            await state.Reset(1);
            Assert.False(wasCalled);
        }

        [Fact]
        public async Task WithGoalScoredEvent()
        {
            var state = CreateGoalScoredScenario();
            var newFixtureEventsHappened = false;
            state.OnNewFixtureEvents += (newEvents) =>
            {
                newFixtureEventsHappened = true;
                Assert.NotEmpty(newEvents.Events);
                Assert.Single(newEvents.Events);
                var goalEvent = newEvents.Events.First().StatMap[StatType.GoalsScored].First();
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
            state.OnPriceChanges += newPrices =>
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
        
        [Fact]
        public async Task WithInjuryUpdate()
        {
            var state = CreateNewInjuryScenario();
            var statusUpdateEmitted = false;
            state.OnStatusUpdates += statusUpdates =>
            {
                statusUpdateEmitted = true;
                Assert.Single(statusUpdates);
                var statusUpdate = statusUpdates.First();
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.True(statusUpdateEmitted);
        }
        
        [Fact]
        public async Task WithNewPlayer()
        {
            var state = CreateNewPlayerScenario();
            var statusUpdateEmitted = false;
            state.OnStatusUpdates += statusUpdates =>
            {
                statusUpdateEmitted = true;
                Assert.Single(statusUpdates);
                var statusUpdate = statusUpdates.First();
                Assert.Equal(statusUpdate.ToPlayer.SecondName, TestBuilder.OtherPlayer().SecondName);
                Assert.Null(statusUpdate.FromPlayer);
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.True(statusUpdateEmitted);
        }
        
        [Fact]
        public async Task WithChangeInDoubtfulnessEmitsEvent()
        {
            var state = CreateChangeInDoubtfulnessScenario();
            var statusUpdateEmitted = false;
            state.OnStatusUpdates += statusUpdates =>
            {
                statusUpdateEmitted = true;
                Assert.Single(statusUpdates);
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.True(statusUpdateEmitted);
        }

        [Fact]
        public async Task WithNoFinishedFixtures_DoesNotEmitEvent()
        {
            var state = CreateNoFinishedFixturesScenario();
            var fixtureFinishedEmitted = false;
            state.OnFixturesProvisionalFinished += fixtures =>
            {
                fixtureFinishedEmitted = true;
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.False(fixtureFinishedEmitted);
        }
        
        [Fact]
        public async Task WithSingleProvisionalFinished_EmitsEvent()
        {
            var state = CreateSingleFinishedFixturesScenario();
            var fixtureFinishedEmitted = false;
            state.OnFixturesProvisionalFinished += fixtures =>
            {
                fixtureFinishedEmitted = true;
                Assert.Single(fixtures);
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.True(fixtureFinishedEmitted);
        }
        
        [Fact]
        public async Task WithMultipleProvisionalFinished_EmitsEvent()
        {
            var state = CreateMultipleFinishedFixturesScenario();
            var fixtureFinishedEmitted = false;
            state.OnFixturesProvisionalFinished += fixtures =>
            {
                fixtureFinishedEmitted = true;
                Assert.Equal(2,fixtures.Count());
                return Task.CompletedTask;
            };
            
            await state.Reset(1);
            await state.Refresh(1);
            Assert.True(fixtureFinishedEmitted);
        }

        private static IState CreateAllMockState()
        {
            return new State(A.Fake<IFixtureClient>(),
                A.Fake<IGlobalSettingsClient>());
        }
        
        private static IState CreateMultipleFinishedFixturesScenario()
        {
            var playerClient = A.Fake<IGlobalSettingsClient>();

            
            var fixtureClient = A.Fake<IFixtureClient>();
            A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
                {
                    TestBuilder.AwayTeamGoal(888, 1),
                    TestBuilder.AwayTeamGoal(999, 1),
                }).Once()
                .Then.Returns(
                    new List<Fixture>
                    {
                        TestBuilder.AwayTeamGoal(888, 1).FinishedProvisional(),
                        TestBuilder.AwayTeamGoal(999, 1).FinishedProvisional(),
                    });
            
            return CreateBaseScenario(fixtureClient, playerClient);
        }
        
        private static IState CreateSingleFinishedFixturesScenario()
        {
            var playerClient = A.Fake<IGlobalSettingsClient>();

            
            var fixtureClient = A.Fake<IFixtureClient>();
            A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
                {
                    TestBuilder.AwayTeamGoal(888, 1),
                    TestBuilder.AwayTeamGoal(999, 1)
                }).Once()
                .Then.Returns(
                    new List<Fixture>
                    {
                        TestBuilder.AwayTeamGoal(888, 1),
                        TestBuilder.AwayTeamGoal(999, 1).FinishedProvisional()
                    });
            
            return CreateBaseScenario(fixtureClient, playerClient);
        }
        
        private static IState CreateNoFinishedFixturesScenario()
        {
            var playerClient = A.Fake<IGlobalSettingsClient>();

            
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
            
            return CreateBaseScenario(fixtureClient, playerClient);
        }
        
        private static IState CreateGoalScoredScenario()
        {
            var playerClient = A.Fake<IGlobalSettingsClient>();
            A.CallTo(() => playerClient.GetGlobalSettings()).Returns(
                new GlobalSettings 
                {
                    Teams =  new List<Team>
                    {
                        TestBuilder.HomeTeam(),
                        TestBuilder.AwayTeam()
                    },
                    Players = new List<Player>
                    {
                        TestBuilder.Player()
                    }
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
            
            return CreateBaseScenario(fixtureClient, playerClient);
        }
        
        private static IState CreateNewInjuryScenario()
        {
            var settingsClient = A.Fake<IGlobalSettingsClient>();
            A.CallTo(() => settingsClient.GetGlobalSettings()).Returns(new GlobalSettings 
                { 
                    Teams = new List<Team>
                    {
                        TestBuilder.HomeTeam(),
                        TestBuilder.AwayTeam()
                    },
                    Players = new List<Player>
                    {
                        TestBuilder.Player().WithStatus(PlayerStatuses.Available)
                    }
                }
            ).Once().Then.Returns(new GlobalSettings 
            { 
                Teams = new List<Team>
                {
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                },
                Players = new List<Player>
                {
                    TestBuilder.Player().WithStatus(PlayerStatuses.Injured)
                }
            });

            var fixtureClient = A.Fake<IFixtureClient>();

            return CreateBaseScenario(fixtureClient, settingsClient);
        }
        
        private static IState CreateChangeInDoubtfulnessScenario()
        {
            var settingsClient = A.Fake<IGlobalSettingsClient>();
            A.CallTo(() => settingsClient.GetGlobalSettings()).Returns(new GlobalSettings 
                { 
                    Teams = new List<Team>
                    {
                        TestBuilder.HomeTeam(),
                        TestBuilder.AwayTeam()
                    },
                    Players = new List<Player>
                    {
                        TestBuilder.Player().WithStatus(PlayerStatuses.Doubtful).WithNews("Knock - 75% chance of playing"),
                    }
                }
            ).Once().Then.Returns(new GlobalSettings 
            { 
                Teams = new List<Team>
                {
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                },
                Players = new List<Player>
                {
                    TestBuilder.Player().WithStatus(PlayerStatuses.Doubtful).WithNews("Knock - 25% chance of playing")
                }
            });

            var fixtureClient = A.Fake<IFixtureClient>();

            return CreateBaseScenario(fixtureClient, settingsClient);
        }
        
        private static IState CreateNewPlayerScenario()
        {
            var settingsClient = A.Fake<IGlobalSettingsClient>();
            A.CallTo(() => settingsClient.GetGlobalSettings()).Returns(new GlobalSettings 
                { 
                    Teams = new List<Team>
                    {
                        TestBuilder.HomeTeam(),
                        TestBuilder.AwayTeam()
                    },
                    Players = new List<Player>
                    {
                        TestBuilder.Player().WithStatus(PlayerStatuses.Available)
                    }
                }
            ).Once().Then.Returns(new GlobalSettings 
            { 
                Teams = new List<Team>
                {
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                },
                Players = new List<Player>
                {
                    TestBuilder.Player().WithStatus(PlayerStatuses.Available),
                    TestBuilder.OtherPlayer().WithStatus(PlayerStatuses.Available)                }
            });

            var fixtureClient = A.Fake<IFixtureClient>();

            return CreateBaseScenario(fixtureClient, settingsClient);
        }

        private static IState CreatePriceIncreaseScenario()
        {
            var playerClient = A.Fake<IGlobalSettingsClient>();
            A.CallTo(() => playerClient.GetGlobalSettings()).Returns(new GlobalSettings 
            { 
                Teams = new List<Team>
                {
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                },
                Players = new List<Player>
                {
                    TestBuilder.Player()
                }
            }
            ).Once().Then.Returns(new GlobalSettings 
            { 
                Teams = new List<Team>
                {
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                },
                Players = new List<Player>
                {
                    TestBuilder.Player().WithCostChangeEvent(1)
                }
            });
            
            var fixtureClient = A.Fake<IFixtureClient>();

            return CreateBaseScenario(fixtureClient, playerClient);
        }

        private static IState CreateBaseScenario(IFixtureClient fixtureClient, IGlobalSettingsClient settingsClient)
        {
            var slackTeamRepository = A.Fake<ISlackTeamRepository>();
            A.CallTo(() => slackTeamRepository.GetAllTeams()).Returns(new List<SlackTeam>
            {
                TestBuilder.SlackTeam()
            });

            return new State(fixtureClient, settingsClient);
        }

        private static ILogger<State> FakeLogger()
        {
            return new Logger<State>(A.Fake<ILoggerFactory>());
        }
    }
}