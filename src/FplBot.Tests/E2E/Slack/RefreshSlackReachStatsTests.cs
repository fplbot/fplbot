using System.Net;
using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.Tests.E2E.Slack;

[Collection("App")]
public class RefreshSlackReachStatsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.ResetChannelOutcomes();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FansOutAndStoresMemberCountPerChannel()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;
        fixture.SetSlackChannelMemberCount(channelId, 15);

        var consumedBefore = fixture.ConsumedSoFar;
        await fixture.Bus.Publish(new RefreshSlackReachStats(), TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle(consumedBefore, published: 2); // dispatch + 1 per-channel

        var counts = await fixture.ChannelMemberCountRepo.GetAll();
        Assert.Equal(15, counts[channelId].MemberCount);
    }

    [Fact]
    public async Task FailingChannel_IsSkipped()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;
        fixture.SlackChannelFails(channelId, "channel_not_found");

        var consumedBefore = fixture.ConsumedSoFar;
        await fixture.Bus.Publish(new RefreshSlackReachStats(), TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle(consumedBefore, published: 2);

        var counts = await fixture.ChannelMemberCountRepo.GetAll();
        Assert.False(counts.ContainsKey(channelId));
    }

    [Fact]
    public async Task AdminRefreshEndpoint_TriggersTheSameFanOut()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;
        fixture.SetSlackChannelMemberCount(channelId, 42);

        var consumedBefore = fixture.ConsumedSoFar;
        var response = await fixture.Post("/api/admin/slack/reach/refresh");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await fixture.WaitUntilBusIdle(consumedBefore, published: 2);

        var counts = await fixture.ChannelMemberCountRepo.GetAll();
        Assert.Equal(42, counts[channelId].MemberCount);
    }
}
