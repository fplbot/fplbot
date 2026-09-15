using FplBot.Data;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class AdminDiscordEndpointsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetSubscriptions_ReturnsGuildsWithTheirChannelSubscriptions()
    {
        var installedGuild = await fixture.SeedGuildInstallation(12345, [EventSubscription.Standings]);

        var result = await AdminDiscordEndpoints.GetSubscriptions(null, 1, 25, fixture.GuildRepo);

        var ok = Assert.IsType<Ok<PagedResult<GuildWithSubsDto>>>(result);
        var guild = Assert.Single(ok.Value!.Items, g => g.GuildId == installedGuild.Id);
        var sub = Assert.Single(guild.Subscriptions);
        Assert.Equal(12345, sub.LeagueId);
        Assert.Contains(EventSubscription.Standings, sub.Subscriptions);
    }

    [Fact]
    public async Task GetSubscriptions_FiltersByGuildId()
    {
        var matching = await fixture.SeedGuildInstallation();
        await fixture.SeedGuildInstallation();

        var result = await AdminDiscordEndpoints.GetSubscriptions(matching.Id, 1, 25, fixture.GuildRepo);

        var ok = Assert.IsType<Ok<PagedResult<GuildWithSubsDto>>>(result);
        var guild = Assert.Single(ok.Value!.Items);
        Assert.Equal(matching.Id, guild.GuildId);
    }

    [Fact]
    public async Task DeleteSubscription_RemovesJustThatChannel()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var result = await AdminDiscordEndpoints.DeleteSubscription(installedGuild.Id, channelId, fixture.GuildRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var remaining = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.DoesNotContain(remaining!.ChannelSubscriptions, c => c.ChannelId == channelId);
    }

    [Fact]
    public async Task DeleteAllSubscriptionsForGuild_RemovesChannelsButKeepsGuildInstalled()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var result = await AdminDiscordEndpoints.DeleteAllSubscriptionsForGuild(installedGuild.Id, fixture.GuildRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var remaining = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.NotNull(remaining);
        Assert.Empty(remaining.ChannelSubscriptions);
    }

    [Fact]
    public async Task DeleteGuild_RemovesGuildEntirely()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var result = await AdminDiscordEndpoints.DeleteGuild(installedGuild.Id, fixture.GuildRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        Assert.Null(await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id));
    }
}
