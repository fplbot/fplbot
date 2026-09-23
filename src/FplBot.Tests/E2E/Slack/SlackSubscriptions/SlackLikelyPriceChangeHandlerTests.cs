using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class SlackLikelyPriceChangeHandlerTests(AppFixture fixture, ITestOutputHelper output) : IAsyncLifetime
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
    public async Task LikelyPriceChange_WorkspaceSubscribedToLikelyPriceChanges_ReceivesSlackMessage()
    {
        await SeedTeam(_teamId!, "#prices", FplEvent.LikelyPriceChanges);

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");

        Assert.Equal("#prices", msg.Channel);
        Assert.Contains("Haaland", msg.Text);
    }

    [Fact]
    public async Task LikelyPriceChange_WorkspaceSubscribedToPriceChangesOnly_NoSlackMessage()
    {
        await SeedTeam(_teamId!, "#prices", FplEvent.PriceChanges);

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage());
    }

    [Fact]
    public async Task LikelyPriceChange_WorkspaceSubscribedToAll_ReceivesSlackMessage()
    {
        await SeedTeam(_teamId!, "#prices", FplEvent.All);

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Equal("#prices", msg.Channel);
        Assert.Contains("Haaland", msg.Text);
    }

    [Fact]
    public async Task LikelyPriceChange_WorkspaceSubscribedToOtherEvents_NoSlackMessage()
    {
        await SeedTeam(_teamId!, "#main", FplEvent.Standings, FplEvent.InjuryUpdates);

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
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
