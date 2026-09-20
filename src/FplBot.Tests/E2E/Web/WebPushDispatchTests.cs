using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Web;

[Collection("App")]
public class WebPushDispatchTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.WebPushCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task InjuryUpdate_SubscribedSubscriber_GetsOneNotification()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new InjuryUpdateOccured([
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Injury", push.Title);
        Assert.Null(push.Link);
    }

    [Fact]
    public async Task GameweekFinished_SubscriberWithoutLeague_GetsNothing()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: null, endpoint: endpoint);

        await fixture.Bus.Publish(new GameweekFinished(new FinishedGameweek(12)), TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle();

        Assert.False(fixture.WebPushCapture.Any());
    }

    [Fact]
    public async Task GameweekFinished_SubscriberWithLeague_GetsStandingsWithLeagueLink()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new GameweekFinished(new FinishedGameweek(12)), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("12", push.Title);
        Assert.Equal("/leagues/123", push.Link);
    }

    [Fact]
    public async Task GameweekJustBegan_SubscriberWithLeague_GetsGameweekStartedWithLeagueLink()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new GameweekJustBegan(new NewGameweek(12)), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("12", push.Title);
        Assert.Equal("/leagues/123", push.Link);
    }

    [Fact]
    public async Task PriceChanges_SubscribedSubscriber_GetsCount()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(1, "Salah", 1, 130, 25.0, 14, "LIV"),
            new PlayerWithPriceChange(2, "Saka", -1, 90, 15.0, 1, "ARS")
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Price", push.Title);
        Assert.Contains("2", push.Body);
    }

    [Fact]
    public async Task TwentyFourHoursToDeadline_SubscribedSubscriber_GetsDeadlineReminder()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new TwentyFourHoursToDeadline(
            new GameweekNearingDeadline(12, "Gameweek 12", DateTime.UtcNow.AddHours(24))), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Deadline", push.Title);
        Assert.Contains("24 hours", push.Body);
    }

    [Fact]
    public async Task OneHourToDeadline_SubscribedSubscriber_GetsDeadlineReminder()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new OneHourToDeadline(
            new GameweekNearingDeadline(12, "Gameweek 12", DateTime.UtcNow.AddHours(1))), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Deadline", push.Title);
        Assert.Contains("60 minutes", push.Body);
    }

    [Fact]
    public async Task LineupReady_SubscribedSubscriber_GetsTheFixtureTeams()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new LineupReady(new Lineups(1,
            new FormationDetails("Liverpool", "4-3-3", []),
            new FormationDetails("Arsenal", "4-4-2", []))), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Lineups", push.Title);
        Assert.Contains("Liverpool", push.Body);
        Assert.Contains("Arsenal", push.Body);
    }

    [Fact]
    public async Task NewPlayersRegistered_SubscribedSubscriber_GetsCount()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new NewPlayersRegistered([
            new NewPlayer(1, "Zirkzee", 55, 14, "MUN")
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("New players", push.Title);
        Assert.Contains("1", push.Body);
    }

    [Fact]
    public async Task FixtureRemoved_SubscribedSubscriber_GetsPostponedNotification()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new FixtureRemovedFromGameweek(12, new RemovedFixture(1,
            new RemovedTeam(1, "Liverpool", "LIV"),
            new RemovedTeam(2, "Arsenal", "ARS"))), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("postponed", push.Title);
    }
}
