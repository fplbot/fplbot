using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.DependencyInjection;

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
        await SeedTeam(_teamId!, "#prices", EventSubscription.PriceChanges);

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
        await SeedTeam(_teamId!, "#main", EventSubscription.Standings, EventSubscription.InjuryUpdates);

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

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }

    private async Task SeedTeam(string teamId, string channel, params EventSubscription[] subscriptions)
    {
        var installation = SlackInstallation.Install(teamId, "Test Team", "xoxb-test-token");
        installation.Subscribe(channel, subscriptions.Select(s => Enum.Parse<FplEvent>(s.ToString())).ToArray());
        await fixture.Services.GetRequiredService<ISlackTeamRepository>().Save(installation);
    }
}
