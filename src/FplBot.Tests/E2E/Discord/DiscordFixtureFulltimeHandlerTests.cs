using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordFixtureFulltimeHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToFixtureFullTime_PostsFulltimeResult()
    {
        var fixtureCode = 555;
        SetUpFixture(fixtureCode);

        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.FixtureFullTime]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new FixtureFinished(fixtureCode), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("FT:", msg.Title);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToFixtureFullTime_DoesNotPost()
    {
        var fixtureCode = 556;
        SetUpFixture(fixtureCode);

        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new FixtureFinished(fixtureCode), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(TimeSpan.FromMilliseconds(500)));
    }

    private void SetUpFixture(int fixtureCode)
    {
        var fplFixture = TestBuilder.NoGoals(fixtureCode).FinishedProvisional();

        var fixtureClient = fixture.Services.GetRequiredService<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixtures()).Returns(new List<Fixture> { fplFixture });

        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() }
        });
    }
}
