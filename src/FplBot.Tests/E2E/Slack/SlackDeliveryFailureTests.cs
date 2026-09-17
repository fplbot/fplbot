using FplBot.Data;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Slack;

[Collection("App")]
public class SlackDeliveryFailureTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        fixture.ResetChannelOutcomes();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync()
    {
        fixture.ResetChannelOutcomes();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ChannelScopedFailure_RecordsFailure()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;
        fixture.SlackChannelFails(channelId, "channel_not_found");

        await fixture.Bus.Publish(new PublishToSlack(installation.Id, channelId, "hello"),
            TestContext.Current.CancellationToken);

        await WaitForFailureCount(installation.Id, channelId, 1);
    }

    [Fact]
    public async Task DeadlineNotificationChannelScopedFailure_RecordsFailure()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;
        fixture.SlackChannelFails(channelId, "is_archived");

        await fixture.Bus.Publish(
            new PublishDeadlineNotificationToSlackWorkspace(installation.Id, channelId,
                new GameweekNearingDeadline(1, "Gameweek 1", new DateTime(2021, 8, 15, 10, 0, 0, DateTimeKind.Utc))),
            TestContext.Current.CancellationToken);

        await WaitForFailureCount(installation.Id, channelId, 1);
    }

    [Fact]
    public async Task InstallLevelFailure_DoesNotRecordFailure()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;
        fixture.SlackChannelFails(channelId, "account_inactive");

        await fixture.Bus.Publish(new PublishToSlack(installation.Id, channelId, "hello"),
            TestContext.Current.CancellationToken);
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        var sub = await fixture.SlackRepo.GetChannelSubscription(installation.Id, channelId);
        Assert.NotNull(sub);
        Assert.Equal(0, sub.FailureCount);
    }

    [Fact]
    public async Task TransientFailure_DoesNotRecordFailure()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;
        fixture.SlackChannelFails(channelId, "ratelimited");

        await fixture.Bus.Publish(new PublishToSlack(installation.Id, channelId, "hello"),
            TestContext.Current.CancellationToken);
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        var sub = await fixture.SlackRepo.GetChannelSubscription(installation.Id, channelId);
        Assert.NotNull(sub);
        Assert.Equal(0, sub.FailureCount);
    }

    private async Task WaitForFailureCount(string teamId, string channelId, int expected)
    {
        for (var i = 0; i < 100; i++)
        {
            var sub = await fixture.SlackRepo.GetChannelSubscription(teamId, channelId);
            if (sub is not null && sub.FailureCount == expected)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Failure count never reached {expected}");
    }
}
