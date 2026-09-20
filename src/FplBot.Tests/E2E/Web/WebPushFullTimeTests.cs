using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.Web;

[Collection("App")]
public class WebPushFullTimeTests(AppFixture fixture) : IAsyncLifetime
{
    private ICollection<Fixture> _originalFixtures = null!;
    private GlobalSettings? _originalGlobalSettings;

    public async ValueTask InitializeAsync()
    {
        fixture.WebPushCapture.Reset();
        await fixture.FlushRedisAsync();

        _originalFixtures = await fixture.Services.GetRequiredService<IFixtureClient>().GetFixtures() ?? [];
        _originalGlobalSettings = await fixture.Services.GetRequiredService<IGlobalSettingsClient>().GetGlobalSettings();
    }

    public ValueTask DisposeAsync()
    {
        A.CallTo(() => fixture.Services.GetRequiredService<IFixtureClient>().GetFixtures()).Returns(_originalFixtures);
        A.CallTo(() => fixture.Services.GetRequiredService<IGlobalSettingsClient>().GetGlobalSettings()).Returns(_originalGlobalSettings);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task FixtureFinished_SubscribedSubscriber_GetsFullTimeScore()
    {
        var fixtureCode = 557;
        SetUpFixture(fixtureCode);

        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new FixtureFinished(fixtureCode), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("HOM", push.Title);
        Assert.Contains("2-1", push.Title);
        Assert.Contains("AWA", push.Title);
        // No bonus points, defensive contributions, or live stats are seeded for this fixture,
        // so there's nothing to report beyond the score already in the title.
        Assert.Empty(push.Body);
    }

    private void SetUpFixture(int fixtureCode)
    {
        var fplFixture = TestBuilder.NoGoals(fixtureCode).FinishedProvisional();
        fplFixture.HomeTeamScore = 2;
        fplFixture.AwayTeamScore = 1;

        var fixtureClient = fixture.Services.GetRequiredService<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixtures()).Returns([fplFixture]);

        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings { Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()] });
    }
}
