using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class UninstallNotificationTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await fixture.FlushRedisAsync();
        fixture.SlackCapture.Reset();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AutoPurged_DoesNotClaimTheTeamDecidedToLeave()
    {
        await fixture.Bus.Publish(new AppUninstalled("T-purge", "Team Purge", UninstallReason.AutoPurged),
            TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync("#fplbot-notifications");
        Assert.Contains("Team Purge", msg.Text);
        Assert.DoesNotContain("decided to uninstall", msg.Text);
    }

    [Fact]
    public async Task AdminDeleted_DoesNotClaimTheTeamDecidedToLeave()
    {
        await fixture.Bus.Publish(new AppUninstalled("T-admin", "Team Admin", UninstallReason.AdminDeleted),
            TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync("#fplbot-notifications");
        Assert.Contains("Team Admin", msg.Text);
        Assert.DoesNotContain("decided to uninstall", msg.Text);
    }

    [Fact]
    public async Task SelfUninstalled_StillPostsTheOriginalMessage()
    {
        await fixture.Bus.Publish(new AppUninstalled("T-self", "Team Self", UninstallReason.SelfUninstalled),
            TestContext.Current.CancellationToken);

        var msg = await fixture.SlackCapture.WaitForMessageAsync("#fplbot-notifications");
        Assert.Contains("Team Self", msg.Text);
        Assert.Contains("decided to uninstall", msg.Text);
    }
}
