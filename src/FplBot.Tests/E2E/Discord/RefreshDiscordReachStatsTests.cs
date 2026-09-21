using System.Net;
using FplBot.Data;
using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class RefreshDiscordReachStatsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.ResetChannelOutcomes();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FansOutAndStoresApproximateMemberCountPerGuild()
    {
        var first = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var second = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        fixture.SetDiscordGuildMemberCount(first.ExternalId, 10);
        fixture.SetDiscordGuildMemberCount(second.ExternalId, 32);

        var consumedBefore = fixture.ConsumedSoFar;
        await fixture.Bus.Publish(new RefreshDiscordReachStats(), TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle(consumedBefore, published: 3); // dispatch + 2 per-guild

        var counts = await fixture.GuildMemberCountRepo.GetAll();
        Assert.Equal(10, counts[first.ExternalId].ApproximateMemberCount);
        Assert.Equal(32, counts[second.ExternalId].ApproximateMemberCount);
    }

    [Fact]
    public async Task FailingGuild_IsSkipped_SiblingStillUpdates()
    {
        var failing = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var healthy = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        fixture.DiscordGuildGetFails(failing.ExternalId, HttpStatusCode.Forbidden);
        fixture.SetDiscordGuildMemberCount(healthy.ExternalId, 7);

        var consumedBefore = fixture.ConsumedSoFar;
        await fixture.Bus.Publish(new RefreshDiscordReachStats(), TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle(consumedBefore, published: 3);

        var counts = await fixture.GuildMemberCountRepo.GetAll();
        Assert.False(counts.ContainsKey(failing.ExternalId));
        Assert.Equal(7, counts[healthy.ExternalId].ApproximateMemberCount);
    }

    [Fact]
    public async Task FansOutAndStoresIsCommunityPerGuild()
    {
        var community = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var privateGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        fixture.SetDiscordGuildIsCommunity(community.ExternalId, true);

        var consumedBefore = fixture.ConsumedSoFar;
        await fixture.Bus.Publish(new RefreshDiscordReachStats(), TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle(consumedBefore, published: 3);

        var counts = await fixture.GuildMemberCountRepo.GetAll();
        Assert.True(counts[community.ExternalId].IsCommunity);
        Assert.False(counts[privateGuild.ExternalId].IsCommunity);
    }

    [Fact]
    public async Task AdminRefreshEndpoint_TriggersTheSameFanOut()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        fixture.SetDiscordGuildMemberCount(guild.ExternalId, 99);

        var consumedBefore = fixture.ConsumedSoFar;
        var response = await fixture.Post("/api/admin/discord/reach/refresh");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await fixture.WaitUntilBusIdle(consumedBefore, published: 2); // dispatch + 1 per-guild

        var counts = await fixture.GuildMemberCountRepo.GetAll();
        Assert.Equal(99, counts[guild.ExternalId].ApproximateMemberCount);
    }
}
