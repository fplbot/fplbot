using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.GameweekLifecycle;
using FplBot.Core.Models;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using FplBot.Messaging.Contracts.Events.v1;
using MediatR;
using NServiceBus;
using NServiceBus.Testing;
using Xunit;

namespace FplBot.Tests
{
    public class StateTests
    {
        private static IMediator _Mediator;
        private static TestableMessageSession _MessageSession;


        [Fact]
        public async Task DoesNotCrashWithNoDataReturned()
        {
            var state = CreateAllMockState();

            await state.Reset(1);
            A.CallTo(() => _Mediator.Publish(null, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        }

        [Fact]
        public async Task WithGoalScoredEvent()
        {
            var state = CreateGoalScoredScenario();
            await state.Reset(1);
            await state.Refresh(1);
            A.CallTo(() => _Mediator.Publish(A<FixtureEventsOccured>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithPriceIncrease()
        {
            var state = CreatePriceIncreaseScenario();
            await state.Reset(1);
            await state.Refresh(1);
            Assert.Single(_MessageSession.PublishedMessages);
            Assert.IsType<PlayersPriceChanged>(_MessageSession.PublishedMessages[0].Message);
        }

        [Fact]
        public async Task WithInjuryUpdate()
        {
            var state = CreateNewInjuryScenario();
            await state.Reset(1);
            await state.Refresh(1);
            A.CallTo(() => _Mediator.Publish(A<InjuryUpdateOccured>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNewPlayer()
        {
            var state = CreateNewPlayerScenario();
            await state.Reset(1);
            await state.Refresh(1);
            A.CallTo(() => _Mediator.Publish(A<InjuryUpdateOccured>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithChangeInDoubtfulnessEmitsEvent()
        {
            var state = CreateChangeInDoubtfulnessScenario();
            await state.Reset(1);
            await state.Refresh(1);
            A.CallTo(() => _Mediator.Publish(A<InjuryUpdateOccured>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNoFinishedFixtures_DoesNotEmitEvent()
        {
            var state = CreateNoFinishedFixturesScenario();
            await state.Reset(1);
            await state.Refresh(1);
            A.CallTo(() => _Mediator.Publish(null, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        }

        [Fact]
        public async Task WithSingleProvisionalFinished_EmitsEvent()
        {
            var state = CreateSingleFinishedFixturesScenario();
            await state.Reset(1);
            await state.Refresh(1);
            A.CallTo(() => _Mediator.Publish(A<FixturesFinished>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithMultipleProvisionalFinished_EmitsEvent()
        {
            var state = CreateMultipleFinishedFixturesScenario();
            await state.Reset(1);
            await state.Refresh(1);
            A.CallTo(() => _Mediator.Publish(A<FixturesFinished>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        private static State CreateAllMockState()
        {
            _Mediator = A.Fake<IMediator>();
            _MessageSession = new TestableMessageSession();
            return new State(A.Fake<IFixtureClient>(),A.Fake<IGlobalSettingsClient>(), _Mediator, _MessageSession);
        }

        private static State CreateMultipleFinishedFixturesScenario()
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

        private static State CreateSingleFinishedFixturesScenario()
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
                        TestBuilder.Player(),
                        TestBuilder.OtherPlayer()
                    }
                });

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
                            .WithProvisionalBonus(TestBuilder.Player().Id, 10)
                            .WithProvisionalBonus(TestBuilder.OtherPlayer().Id, 20)
                    });

            return CreateBaseScenario(fixtureClient, playerClient);
        }

        private static State CreateNoFinishedFixturesScenario()
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

        private static State CreateGoalScoredScenario()
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

        private static State CreateNewInjuryScenario()
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

        private static State CreateChangeInDoubtfulnessScenario()
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

        private static State CreateNewPlayerScenario()
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
                    TestBuilder.OtherPlayer().WithStatus(PlayerStatuses.Available)
                }
            });

            var fixtureClient = A.Fake<IFixtureClient>();

            return CreateBaseScenario(fixtureClient, settingsClient);
        }

        private static State CreatePriceIncreaseScenario()
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

        private static State CreateBaseScenario(IFixtureClient fixtureClient, IGlobalSettingsClient settingsClient)
        {
            var slackTeamRepository = A.Fake<ISlackTeamRepository>();
            A.CallTo(() => slackTeamRepository.GetAllTeams()).Returns(new List<SlackTeam>
            {
                TestBuilder.SlackTeam()
            });
            _Mediator = A.Fake<IMediator>();
            _MessageSession = new TestableMessageSession();
            return new State(fixtureClient, settingsClient, _Mediator, _MessageSession);
        }


    }
}
