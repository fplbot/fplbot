using Fpl.Client.Abstractions;
using FplBot.Data;
using FplBot.Domain;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using FakeItEasy;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class AdminDiscordEndpointsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }
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
    public async Task GetSubscriptions_IncludesFailureState()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        var failingSince = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        guild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince);
        guild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince.AddDays(1));
        await fixture.GuildRepo.Save(guild);

        var result = await AdminDiscordEndpoints.GetSubscriptions(null, 1, 25, fixture.GuildRepo);

        var ok = Assert.IsType<Ok<PagedResult<GuildWithSubsDto>>>(result);
        var dto = ok.Value!.Items.Single(g => g.GuildId == guild.Id).Subscriptions.Single();
        Assert.Equal(2, dto.FailureCount);
        Assert.Equal(failingSince, dto.FailingSince);
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

    [Fact]
    public async Task GetGuild_ReturnsGuildNameAndChannels()
    {
        var installedGuild = await fixture.SeedGuildInstallation(12345, [EventSubscription.Standings]);
        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();

        var result = await AdminDiscordEndpoints.GetGuild(installedGuild.Id, fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient, NullLogger<Program>.Instance);

        var ok = Assert.IsAssignableFrom<IValueHttpResult>(result);
        dynamic value = ok.Value!;
        Assert.Equal(installedGuild.Id, (string)value.guildId);
    }

    [Fact]
    public async Task GetGuild_IncludesFailureState()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        var failingSince = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        installedGuild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince);
        installedGuild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince.AddDays(1));
        await fixture.GuildRepo.Save(installedGuild);

        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();
        var result = await AdminDiscordEndpoints.GetGuild(installedGuild.Id, fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient, NullLogger<Program>.Instance);

        var ok = Assert.IsAssignableFrom<IValueHttpResult>(result);
        dynamic value = ok.Value!;
        dynamic channel = Assert.Single((IEnumerable<object>)value.channels);
        Assert.Equal(2, (int)channel.failureCount);
        Assert.Equal(failingSince, (DateTimeOffset?)channel.failingSince);
    }

    [Fact]
    public async Task GetGuild_GuildNotFound_ReturnsNotFound()
    {
        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();

        var result = await AdminDiscordEndpoints.GetGuild(Guid.NewGuid().ToString("N"), fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient, NullLogger<Program>.Instance);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task PublishStandings_ChannelNotFollowingLeague_DoesNotPublish()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: null, subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var result = await AdminDiscordEndpoints.PublishStandings(
            installedGuild.Id, channelId, fixture.GuildRepo, fixture.Publisher, A.Fake<IGlobalSettingsClient>());

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        Assert.False((bool)value.published);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(channelId, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_AddsAndRemovesToMatchRequestedSet()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings, EventSubscription.Captains]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var request = new UpdateGuildChannelSubscriptionsRequest([EventSubscription.Captains, EventSubscription.Deadlines]);
        var result = await AdminDiscordEndpoints.UpdateChannelSubscriptions(installedGuild.Id, channelId, request, fixture.GuildRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        var channel = updated!.GetChannel(channelId)!;
        Assert.Equal(
            new[] { FplEvent.Captains, FplEvent.Deadlines }.OrderBy(e => e),
            channel.Events.Current.OrderBy(e => e));
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_GuildNotFound_ReturnsNotFound()
    {
        var result = await AdminDiscordEndpoints.UpdateChannelSubscriptions(Guid.NewGuid().ToString("N"), "channel-1", new UpdateGuildChannelSubscriptionsRequest([]), fixture.GuildRepo);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task MoveChannel_MovesSubscriptionAndDeletesOldStorageKey()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var result = await AdminDiscordEndpoints.MoveChannel(installedGuild.Id, oldChannelId, new MoveGuildChannelRequest("new-channel"), fixture.GuildRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.Null(updated!.GetChannel(oldChannelId));
        Assert.Equal(123, (int)updated.GetChannel("new-channel")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task MoveChannel_ChannelNotFound_ReturnsNotFound()
    {
        var installedGuild = await fixture.SeedGuildInstallation();

        var result = await AdminDiscordEndpoints.MoveChannel(installedGuild.Id, "missing-channel", new MoveGuildChannelRequest("new-channel"), fixture.GuildRepo);

        Assert.IsType<NotFound>(result);
    }
}
