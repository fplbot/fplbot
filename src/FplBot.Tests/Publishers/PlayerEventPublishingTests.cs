using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.RecurringActions;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.Publishers;

public class PlayerEventPublishingTests
{
    private static TestPublishEndpoint _messageSession = null!;

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
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
            {
                Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
                Players = new List<Player>
                {
                    TestBuilder.Player().FromHomeTeam(), TestBuilder.OtherPlayer().FromAwayTeam()
                }
            },
            new GlobalSettings
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
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
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
            },
            new GlobalSettings
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
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
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
            },
            new GlobalSettings
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
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
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
            },
            new GlobalSettings
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
        var playerClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
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
            },
            new GlobalSettings
            {
                Teams = new List<Team>
                {
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                },
                Players = new List<Player>
                {
                    TestBuilder.Player().WithCost(1)
                }
            });



        return CreatePlayerBaseScenario(playerClient);
    }

    private static PlayerUpdatesRecurringAction CreatePlayerBaseScenario(IGlobalSettingsClient playerClient)
    {
        _messageSession = new TestPublishEndpoint();
        return new PlayerUpdatesRecurringAction(playerClient, new TestScopeFactory(_messageSession), A.Fake<ILogger<PlayerUpdatesRecurringAction>>());
    }
}
