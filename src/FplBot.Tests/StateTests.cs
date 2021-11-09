using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Workers.States;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;


namespace FplBot.Tests;

public class StateTests
{
    private static TestableMessageSession _messageSession;

    [Fact]
    public async Task DoesNotCrashWithNoDataReturned()
    {
        var state = CreateAllMockState();

        await state.Reset(1);
        Assert.Empty(_messageSession.PublishedMessages);
    }

    [Fact]
    public async Task WithGoalScoredEvent()
    {
        var state = CreateGoalScoredScenario();
        await state.Reset(1);
        await state.Refresh(1);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<FixtureEventsOccured>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithPriceIncrease()
    {
        var state = CreatePriceIncreaseScenario();
        await state.Reset(1);
        await state.Refresh(1);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<PlayersPriceChanged>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithInjuryUpdate()
    {
        var state = CreateNewInjuryScenario();
        await state.Reset(1);
        await state.Refresh(1);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<InjuryUpdateOccured>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithNewPlayer()
    {
        var state = CreateNewPlayerScenario();
        await state.Reset(1);
        await state.Refresh(1);

        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<NewPlayersRegistered>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithChangeInDoubtfulnessEmitsEvent()
    {
        var state = CreateChangeInDoubtfulnessScenario();
        await state.Reset(1);
        await state.Refresh(1);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<InjuryUpdateOccured>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithNoFinishedFixtures_DoesNotEmitEvent()
    {
        var state = CreateNoFinishedFixturesScenario();
        await state.Reset(1);
        await state.Refresh(1);

        Assert.Empty(_messageSession.PublishedMessages.Containing<FixtureFinished>());
    }

    [Fact]
    public async Task WithSingleProvisionalFinished_EmitsEvent()
    {
        var state = CreateSingleFinishedFixturesScenario();
        await state.Reset(1);
        await state.Refresh(1);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<FixtureFinished>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithMultipleProvisionalFinished_EmitsEvent()
    {
        var state = CreateMultipleFinishedFixturesScenario();
        await state.Reset(1);
        await state.Refresh(1);
        Assert.Equal(2, _messageSession.PublishedMessages.Length);
        Assert.IsType<FixtureFinished>(_messageSession.PublishedMessages[0].Message);
        Assert.IsType<FixtureFinished>(_messageSession.PublishedMessages[1].Message);
    }

    private static State CreateAllMockState()
    {
        _messageSession = new TestableMessageSession();
        return new State(A.Fake<IFixtureClient>(),A.Fake<IGlobalSettingsClient>(), _messageSession, A.Fake<ILogger<State>>());
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

        _messageSession = new TestableMessageSession();
        return new State(fixtureClient, settingsClient, _messageSession, A.Fake<ILogger<State>>());
    }


}