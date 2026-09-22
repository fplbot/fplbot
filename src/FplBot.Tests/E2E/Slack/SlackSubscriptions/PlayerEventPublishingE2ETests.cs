using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.States;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        await state.Tick(CancellationToken.None);
        await state.Tick(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithInjuryUpdate()
    {
        var state = CreateNewInjuryScenario();
        await state.Tick(CancellationToken.None);
        await state.Tick(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithNewPlayer()
    {
        var state = CreateNewPlayerScenario();
        await state.Tick(CancellationToken.None);
        await state.Tick(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("New player", msg.Text);
    }

    [Fact]
    public async Task WithChangeInDoubtfulnessEmitsEvent()
    {
        var state = CreateChangeInDoubtfulnessScenario();
        await state.Tick(CancellationToken.None);
        await state.Tick(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithLikelyPriceChange_ShowcasedToPriceChangesSubscriber()
    {
        var state = CreateLikelyPriceChangeScenario();
        await state.Tick(CancellationToken.None);
        await state.Tick(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithPlayerTransferBetweenTwoPLTeams_EmitsEvent()
    {
        var messageSession = new TestPublishEndpoint();
        var state = CreateTeamChangeScenario(messageSession);
        await state.Tick(CancellationToken.None);
        await state.Tick(CancellationToken.None);

        Assert.Single(messageSession.PublishedMessages);
        Assert.IsType<PremiershipPlayerTransferred>(messageSession.PublishedMessages[0].Message);
    }

    private static PlayerUpdatesMonitor CreateTeamChangeScenario(TestPublishEndpoint messageSession)
    {
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
        {
            Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
            Players =
                [
                    TestBuilder.Player().FromHomeTeam(), TestBuilder.OtherPlayer().FromAwayTeam()
                ]
        },
            new GlobalSettings
            {
                Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
                Players =
                [
                    TestBuilder.Player().FromAwayTeam(), TestBuilder.OtherPlayer().FromAwayTeam()
                ]
            });

        return new PlayerUpdatesMonitor(settingsClient, new TestScopeFactory(messageSession), NullLogger<PlayerUpdatesMonitor>.Instance);
    }

    private PlayerUpdatesMonitor CreateNewInjuryScenario()
    {
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
        {
            Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
            Players =
                [
                    TestBuilder.Player().WithStatus(PlayerStatuses.Available)
                ]
        },
            new GlobalSettings
            {
                Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
                Players =
                [
                    TestBuilder.Player().WithStatus(PlayerStatuses.Injured)
                ]
            });

        return CreatePlayerBaseScenario(settingsClient);
    }

    private PlayerUpdatesMonitor CreateChangeInDoubtfulnessScenario()
    {
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
        {
            Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
            Players =
                [
                    TestBuilder.Player().WithStatus(PlayerStatuses.Doubtful).WithNews("Knock - 75% chance of playing"),
                ]
        },
            new GlobalSettings
            {
                Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
                Players =
                [
                    TestBuilder.Player().WithStatus(PlayerStatuses.Doubtful).WithNews("Knock - 25% chance of playing")
                ]
            });

        return CreatePlayerBaseScenario(settingsClient);
    }

    private PlayerUpdatesMonitor CreateNewPlayerScenario()
    {
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
        {
            Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
            Players =
                [
                    TestBuilder.Player().WithStatus(PlayerStatuses.Available)
                ]
        },
            new GlobalSettings
            {
                Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
                Players =
                [
                    TestBuilder.Player().WithStatus(PlayerStatuses.Available),
                    TestBuilder.OtherPlayer().WithStatus(PlayerStatuses.Available)
                ]
            });

        return CreatePlayerBaseScenario(settingsClient);
    }

    private PlayerUpdatesMonitor CreatePriceIncreaseScenario()
    {
        var playerClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
        {
            Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
            Players =
                [
                    TestBuilder.Player()
                ]
        },
            new GlobalSettings
            {
                Teams =
                [
                    TestBuilder.HomeTeam(),
                    TestBuilder.AwayTeam()
                ],
                Players =
                [
                    TestBuilder.Player().WithCost(1)
                ]
            });

        return CreatePlayerBaseScenario(playerClient);
    }

    private PlayerUpdatesMonitor CreateLikelyPriceChangeScenario()
    {
        var settingsClient = GlobalSettingsClientBuilder.Returning(
            new GlobalSettings
            {
                Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
                Players = [TestBuilder.Player()]
            },
            new GlobalSettings
            {
                Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
                Players = [TestBuilder.Player().WithNextPriceChangeLikelihood(5)]
            });

        return CreatePlayerBaseScenario(settingsClient);
    }

    private PlayerUpdatesMonitor CreatePlayerBaseScenario(IGlobalSettingsClient playerClient) =>
        new(playerClient, fixture.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PlayerUpdatesMonitor>.Instance);
}
