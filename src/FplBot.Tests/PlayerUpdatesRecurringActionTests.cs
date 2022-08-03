using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.RecurringActions;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;

namespace FplBot.Tests;

public class PlayerUpdatesRecurringActionTests
{
    private static TestableMessageSession _messageSession;

    [Fact]
    public async Task WithPriceIncrease()
    {
        var state = CreatePriceIncreaseScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<PlayersPriceChanged>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithInjuryUpdate()
    {
        var state = CreateNewInjuryScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<InjuryUpdateOccured>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithNewPlayer()
    {
        var state = CreateNewPlayerScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);

        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<NewPlayersRegistered>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithChangeInDoubtfulnessEmitsEvent()
    {
        var state = CreateChangeInDoubtfulnessScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<InjuryUpdateOccured>(_messageSession.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WithPlayerTransferBetweenTwoPLTeams_EmitsEvent()
    {
        var state = CreateTeamChangeScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);
        Assert.Single(_messageSession.PublishedMessages);
        Assert.IsType<PremiershipPlayerTransferred>(_messageSession.PublishedMessages[0].Message);
    }

    private static PlayerUpdatesRecurringAction CreateTeamChangeScenario()
    {
        var settingsClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => settingsClient.GetGlobalSettings()).Returns(new GlobalSettings
            {
                Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
                Players = new List<Player>
                {
                    TestBuilder.Player().FromHomeTeam(), TestBuilder.OtherPlayer().FromAwayTeam()
                }
            }
        ).Once().Then.Returns(new GlobalSettings
        {
            Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
            Players = new List<Player>
            {
                TestBuilder.Player().FromAwayTeam(), TestBuilder.OtherPlayer().FromAwayTeam()
            }
        });

        return CreatePlayerBaseScenario(settingsClient);
    }

    private static PlayerUpdatesRecurringAction CreateNewInjuryScenario()
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


        return CreatePlayerBaseScenario(settingsClient);
    }

    private static PlayerUpdatesRecurringAction CreateChangeInDoubtfulnessScenario()
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

        return CreatePlayerBaseScenario(settingsClient);
    }

    private static PlayerUpdatesRecurringAction CreateNewPlayerScenario()
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

        return CreatePlayerBaseScenario(settingsClient);
    }

    private static PlayerUpdatesRecurringAction CreatePriceIncreaseScenario()
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



        return CreatePlayerBaseScenario(playerClient);
    }

    private static PlayerUpdatesRecurringAction CreatePlayerBaseScenario(IGlobalSettingsClient playerClient)
    {
        _messageSession = new TestableMessageSession();
        return new PlayerUpdatesRecurringAction(playerClient, _messageSession, A.Fake<ILogger<PlayerUpdatesRecurringAction>>());
    }
}
