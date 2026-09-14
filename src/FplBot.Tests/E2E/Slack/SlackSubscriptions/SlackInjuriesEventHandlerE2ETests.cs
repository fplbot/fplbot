using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class SlackInjuriesEventHandlerE2ETests(AppFixture fixture, ITestOutputHelper output) : IAsyncLifetime
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
    public async Task InjuryUpdate_WorkspaceSubscribedToInjuryUpdates_ReceivesSlackMessage()
    {
        await fixture.InstallSlackbot(_teamId, "T1");
        await fixture.Subscribe(_teamId!, "#injuries", FplEvent.InjuryUpdates);

        await fixture.Bus.Publish(new InjuryUpdateOccured([
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");

        Assert.Equal("#injuries", msg.Channel);
        Assert.Contains("Salah", msg.Text);
    }

    [Fact]
    public async Task InjuryUpdate_WorkspaceNotSubscribed_NoSlackMessage()
    {
        await fixture.InstallSlackbot(_teamId, "T1");
        await fixture.Subscribe(_teamId!, "#injuries", FplEvent.Standings);

        await fixture.Bus.Publish(new InjuryUpdateOccured([
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        ]), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task MultipleWorkspaces_OnlySubscribedOnesReceiveMessage()
    {
        var subscribedTeamId = _teamId;
        var unsubscribedTeamId = Guid.NewGuid().ToString("N");

        await fixture.InstallSlackbot(subscribedTeamId, "T1");
        await fixture.Subscribe(subscribedTeamId!, "#injuries", FplEvent.InjuryUpdates);
        await fixture.InstallSlackbot(unsubscribedTeamId, "T2");
        await fixture.Subscribe(unsubscribedTeamId, "#injuries", FplEvent.Standings);

        await fixture.Bus.Publish(new InjuryUpdateOccured([
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");
        Assert.Equal("#injuries", msg.Channel);

        // No second message should arrive
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }
}
