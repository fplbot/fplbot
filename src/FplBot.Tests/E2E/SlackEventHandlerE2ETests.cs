using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E;

public class SlackEventHandlerE2ETests(EventHandlerFixture fixture, ITestOutputHelper output)
    : IClassFixture<EventHandlerFixture>, IAsyncLifetime
{
    private string? _teamId;

    public async Task InitializeAsync()
    {
        _teamId = Guid.NewGuid().ToString("N");
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InjuryUpdate_WorkspaceSubscribedToInjuryUpdates_ReceivesSlackMessage()
    {
        await SeedTeam(_teamId!, "#injuries", EventSubscription.InjuryUpdates);

        await fixture.Bus.Publish(new InjuryUpdateOccured(new[]
        {
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        }));

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");

        Assert.Equal("#injuries", msg.Channel);
        Assert.Contains("Salah", msg.Text);
    }

    [Fact]
    public async Task InjuryUpdate_WorkspaceNotSubscribed_NoSlackMessage()
    {
        await SeedTeam(_teamId!, "#main", EventSubscription.Standings);

        await fixture.Bus.Publish(new InjuryUpdateOccured(new[]
        {
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        }));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task PriceChange_WorkspaceSubscribedToPriceChanges_ReceivesSlackMessage()
    {
        await SeedTeam(_teamId!, "#prices", EventSubscription.PriceChanges);

        await fixture.Bus.Publish(new PlayersPriceChanged(new List<PlayerWithPriceChange>
        {
            new PlayerWithPriceChange(
                PlayerId: 1,
                WebName: "Haaland",
                CostChangeEvent: 1,
                NowCost: 145,
                OwnershipPercentage: 30.5,
                TeamId: 11,
                TeamShortName: "MCI")
        }));

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");

        Assert.Equal("#prices", msg.Channel);
        Assert.Contains("Haaland", msg.Text);
    }

    [Fact]
    public async Task PriceChange_WorkspaceSubscribedToOtherEvents_NoSlackMessage()
    {
        await SeedTeam(_teamId!, "#main", EventSubscription.Standings, EventSubscription.InjuryUpdates);

        await fixture.Bus.Publish(new PlayersPriceChanged(new List<PlayerWithPriceChange>
        {
            new PlayerWithPriceChange(
                PlayerId: 1,
                WebName: "Haaland",
                CostChangeEvent: 1,
                NowCost: 145,
                OwnershipPercentage: 30.5,
                TeamId: 11,
                TeamShortName: "MCI")
        }));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task MultipleWorkspaces_OnlySubscribedOnesReceiveMessage()
    {
        var subscribedTeamId = _teamId;
        var unsubscribedTeamId = Guid.NewGuid().ToString("N");

        await SeedTeam(subscribedTeamId!, "#injuries", EventSubscription.InjuryUpdates);
        await SeedTeam(unsubscribedTeamId, "#main", EventSubscription.Standings);

        await fixture.Bus.Publish(new InjuryUpdateOccured(new[]
        {
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        }));

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");

        Assert.Equal("#injuries", msg.Channel);

        // No second message should arrive
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }

    private Task SeedTeam(string teamId, string channel, params EventSubscription[] subscriptions)
        => fixture.Store.Insert(new SlackTeam
        {
            TeamId = teamId,
            TeamName = "Test Team",
            AccessToken = "xoxb-test-token",
            FplBotSlackChannel = channel,
            FplbotLeagueId = 123,
            Subscriptions = subscriptions.ToList()
        });
}
