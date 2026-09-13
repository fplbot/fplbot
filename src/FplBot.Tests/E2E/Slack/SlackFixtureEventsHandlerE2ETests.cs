using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class SlackFixtureEventsHandlerE2ETests(AppFixture fixture, ITestOutputHelper output) : IAsyncLifetime
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
    public async Task GoalScored_WorkspaceSubscribedToGoals_ReceivesSlackMessage()
    {
        await SeedTeam(_teamId!, "#goals", EventSubscription.FixtureGoals);

        await fixture.Bus.Publish(new FixtureEventsOccured([GoalFixtureEvent()]), TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync();
        output.WriteLine($"Received: {msg.Text}");

        Assert.Equal("#goals", msg.Channel);
        Assert.Contains("Saka", msg.Text);
    }

    [Fact]
    public async Task GoalScored_WorkspaceNotSubscribedToGoals_NoSlackMessage()
    {
        await SeedTeam(_teamId!, "#assists", EventSubscription.FixtureAssists);

        await fixture.Bus.Publish(new FixtureEventsOccured([GoalFixtureEvent()]), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }

    private static FixtureEvents GoalFixtureEvent() => new(
        new FixtureScore(
            new FixtureTeam(1, "Arsenal", "ARS"),
            new FixtureTeam(2, "Chelsea", "CHE"),
            90, 1, 0),
        new Dictionary<StatType, List<PlayerEvent>>
        {
            [StatType.GoalsScored] = [new PlayerEvent(new PlayerDetails(10, "Saka"), TeamType.Home, false)]
        });

    private async Task SeedTeam(string teamId, string channel, params EventSubscription[] subscriptions)
    {
        var installation = SlackInstallation.Install(teamId, "Test Team", "xoxb-test-token");
        installation.Subscribe(channel, subscriptions.Select(s => Enum.Parse<FplEvent>(s.ToString())).ToArray());
        await fixture.Services.GetRequiredService<ISlackTeamRepository>().Save(installation);
    }
}
