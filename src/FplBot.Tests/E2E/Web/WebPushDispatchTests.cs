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
        Assert.Contains("finished", push.Title, StringComparison.OrdinalIgnoreCase);
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
    public async Task GameweekJustBegan_SubscriberWithOnlyTransfers_GetsGameweekStarted()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);
        var response = await fixture.PutWebPush(subscriberId, "/api/web/me/events", new { events = new[] { "Transfers" } });
        response.EnsureSuccessStatusCode();

        await fixture.Bus.Publish(new GameweekJustBegan(new NewGameweek(12)), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("12", push.Title);
        Assert.Equal("/leagues/123", push.Link);
    }

    [Fact]
    public async Task PriceChanges_SubscribedSubscriber_GetsThePlayerNames()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(1, "Salah", 1, 130, 25.0, 14, "LIV"),
            new PlayerWithPriceChange(2, "Saka", -1, 90, 15.0, 1, "ARS")
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Price", push.Title);
        Assert.Contains("Salah", push.Body);
        Assert.Contains("Saka", push.Body);
    }

    [Fact]
    public async Task PriceChanges_CountsOnlyRelevantPlayers()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(1, "Salah", 1, 130, 25.0, 14, "LIV"),
            new PlayerWithPriceChange(2, "Nobody", 1, 40, -1.0, 1, "ARS")
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Salah", push.Body);
        Assert.DoesNotContain("Nobody", push.Body);
    }

    [Fact]
    public async Task LikelyPriceChanges_SubscribedSubscriber_GetsThePlayerNames()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Likely", push.Title);
        Assert.Contains("Haaland", push.Body);
    }

    [Fact]
    public async Task LikelyPriceChanges_SubscriberOnlySubscribedToPriceChanges_StillGetsNotified()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);
        var response = await fixture.PutWebPush(subscriberId, "/api/web/me/events", new { events = new[] { "PriceChanges" } });
        response.EnsureSuccessStatusCode();

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Haaland", push.Body);
    }

    [Fact]
    public async Task NewPlayersRegistered_OnlyIrrelevantPlayers_GetsNothing()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new NewPlayersRegistered([
            new NewPlayer(1, "Nobody", -1, 14, "MUN")
        ]), TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.WebPushCapture.Any());
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
    public async Task NewPlayersRegistered_SubscribedSubscriber_GetsThePlayerName()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new NewPlayersRegistered([
            new NewPlayer(1, "Zirkzee", 55, 14, "MUN")
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("New player", push.Title);
        Assert.Contains("Zirkzee", push.Body);
        Assert.DoesNotContain("New player", push.Body);
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

    [Fact]
    public async Task TwoGoals_SendsOneNotificationGroupingBoth()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new FixtureEventsOccured([
            new FixtureEvents(
                new FixtureScore(new FixtureTeam(1, "Manchester City", "MCI"),
                    new FixtureTeam(2, "Arsenal", "ARS"), 34, 2, 0),
                new Dictionary<StatType, List<PlayerEvent>>
                {
                    [StatType.GoalsScored] =
                    [
                        new PlayerEvent(new PlayerDetails(1, "Foden"), TeamType.Home, false),
                        new PlayerEvent(new PlayerDetails(2, "Haaland"), TeamType.Home, false)
                    ]
                })
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("2-0", push.Title);
        Assert.Contains("Foden", push.Body);
        Assert.Contains("Haaland", push.Body);
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.WebPushCapture.Any());
    }

    [Fact]
    public async Task GoalAndAssist_SameFixture_SendsOneNotificationCombiningBoth()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new FixtureEventsOccured([
            new FixtureEvents(
                new FixtureScore(new FixtureTeam(1, "Manchester City", "MCI"),
                    new FixtureTeam(2, "Arsenal", "ARS"), 34, 1, 0),
                new Dictionary<StatType, List<PlayerEvent>>
                {
                    [StatType.GoalsScored] = [new PlayerEvent(new PlayerDetails(1, "Haaland"), TeamType.Home, false)],
                    [StatType.Assists] = [new PlayerEvent(new PlayerDetails(2, "De Bruyne"), TeamType.Home, false)]
                })
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Haaland", push.Body);
        Assert.Contains("De Bruyne", push.Body);
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.WebPushCapture.Any());
    }

    [Fact]
    public async Task RedCard_SendsCardNotification()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new FixtureEventsOccured([
            new FixtureEvents(
                new FixtureScore(new FixtureTeam(1, "Manchester City", "MCI"),
                    new FixtureTeam(2, "Arsenal", "ARS"), 60, 1, 0),
                new Dictionary<StatType, List<PlayerEvent>>
                {
                    [StatType.RedCards] = [new PlayerEvent(new PlayerDetails(3, "Gabriel"), TeamType.Away, false)]
                })
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("1-0", push.Title);
        Assert.Contains("Gabriel", push.Body);
        Assert.Contains("red card", push.Body);
    }

    [Fact]
    public async Task RemovedGoal_SendsVarOverturnedNotification()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new FixtureEventsOccured([
            new FixtureEvents(
                new FixtureScore(new FixtureTeam(1, "Manchester City", "MCI"),
                    new FixtureTeam(2, "Arsenal", "ARS"), 40, 1, 0),
                new Dictionary<StatType, List<PlayerEvent>>
                {
                    [StatType.GoalsScored] = [new PlayerEvent(new PlayerDetails(1, "Foden"), TeamType.Home, true)]
                })
        ]), TestContext.Current.CancellationToken);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Foden", push.Body);
        Assert.Contains("VAR", push.Body);
    }

    [Fact]
    public async Task YellowCard_SendsNothing()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        await fixture.Bus.Publish(new FixtureEventsOccured([
            new FixtureEvents(
                new FixtureScore(new FixtureTeam(1, "Manchester City", "MCI"),
                    new FixtureTeam(2, "Arsenal", "ARS"), 55, 1, 0),
                new Dictionary<StatType, List<PlayerEvent>>
                {
                    [StatType.YellowCards] = [new PlayerEvent(new PlayerDetails(4, "Rice"), TeamType.Away, false)]
                })
        ]), TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.WebPushCapture.Any());
    }
}
