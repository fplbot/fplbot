using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class SlackPriceCHangesEventHandlerE2ETests(AppFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
    public required string _teamId;

    public async ValueTask InitializeAsync()
    {
        _teamId = Guid.NewGuid().ToString("N");
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PriceChange_WorkspaceSubscribedToPriceChanges_ReceivesSlackMessage()
    {
        await SeedTeam(_teamId!, "#prices", FplEvent.PriceChanges);

        await fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(
                PlayerId: 1,
                WebName: "Haaland",
                CostChangeEvent: 1,
                NowCost: 145,
                OwnershipPercentage: 30.5,
                TeamId: 11,
                TeamShortName: "MCI")
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");

        Assert.Equal("#prices", msg.Channel);
        Assert.Contains("Haaland", msg.Text);
    }

    [Fact]
    public async Task PriceChange_WorkspaceSubscribedToOtherEvents_NoSlackMessage()
    {
        await SeedTeam(_teamId!, "#main", FplEvent.Standings, FplEvent.InjuryUpdates);

        await fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(
                PlayerId: 1,
                WebName: "Haaland",
                CostChangeEvent: 1,
                NowCost: 145,
                OwnershipPercentage: 30.5,
                TeamId: 11,
                TeamShortName: "MCI")
        ]), TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage());
    }

    private async Task SeedTeam(string teamId, string channel, params FplEvent[] events)
    {
        await fixture.InstallSlackbot(teamId, "T1");
        await fixture.Subscribe(teamId, channel, events);
    }
}
