using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.RecurringActions;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class PlayerEventPublishingE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private const string Channel = "#players";

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
        var installation = SlackInstallationFaker.Generate();
        installation.Subscribe(Channel, [FplEvent.PriceChanges, FplEvent.InjuryUpdates, FplEvent.NewPlayers]);
        await fixture.Services.GetRequiredService<ISlackTeamRepository>().Save(installation);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WithPriceIncrease()
    {
        var state = CreatePriceIncreaseScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithInjuryUpdate()
    {
        var state = CreateNewInjuryScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithNewPlayer()
    {
        var state = CreateNewPlayerScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("New player", msg.Text);
    }

    [Fact]
    public async Task WithChangeInDoubtfulnessEmitsEvent()
    {
        var state = CreateChangeInDoubtfulnessScenario();
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithPlayerTransferBetweenTwoPLTeams_EmitsEvent()
    {
        var messageSession = new TestPublishEndpoint();
        var state = CreateTeamChangeScenario(messageSession);
        await state.Process(CancellationToken.None);
        await state.Process(CancellationToken.None);

        Assert.Single(messageSession.PublishedMessages);
        Assert.IsType<PremiershipPlayerTransferred>(messageSession.PublishedMessages[0].Message);
    }

    private static PlayerUpdatesRecurringAction CreateTeamChangeScenario(TestPublishEndpoint messageSession)
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

        return new PlayerUpdatesRecurringAction(settingsClient, new TestScopeFactory(messageSession), A.Fake<ILogger<PlayerUpdatesRecurringAction>>());
    }

    private PlayerUpdatesRecurringAction CreateNewInjuryScenario()
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

    private PlayerUpdatesRecurringAction CreateChangeInDoubtfulnessScenario()
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

    private PlayerUpdatesRecurringAction CreateNewPlayerScenario()
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

    private PlayerUpdatesRecurringAction CreatePriceIncreaseScenario()
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

    private PlayerUpdatesRecurringAction CreatePlayerBaseScenario(IGlobalSettingsClient playerClient) =>
        new(playerClient, fixture.Services.GetRequiredService<IServiceScopeFactory>(), A.Fake<ILogger<PlayerUpdatesRecurringAction>>());
}
