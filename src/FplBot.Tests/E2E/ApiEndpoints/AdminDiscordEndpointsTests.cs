using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
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
        fixture.ResetChannelOutcomes();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetSubscriptions_ReturnsGuildsWithTheirChannelSubscriptions()
    {
        var installedGuild = await fixture.SeedGuildInstallation(12345, [EventSubscription.Standings]);

        var result = await AdminDiscordEndpoints.GetSubscriptions(null, 1, 25, false, fixture.GuildRepo);

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
        guild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince, "50013");
        guild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince.AddDays(1), "50013");
        await fixture.GuildRepo.Save(guild);

        var result = await AdminDiscordEndpoints.GetSubscriptions(null, 1, 25, false, fixture.GuildRepo);

        var ok = Assert.IsType<Ok<PagedResult<GuildWithSubsDto>>>(result);
        var dto = ok.Value!.Items.Single(g => g.GuildId == guild.Id).Subscriptions.Single();
        Assert.Equal(2, dto.FailureCount);
        Assert.Equal(failingSince, dto.FailingSince);
        Assert.Equal("50013", dto.LastFailureReason);
    }

    [Fact]
    public async Task GetSubscriptions_FailingOnly_ExcludesHealthyGuilds()
    {
        var failing = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var healthy = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        failing.GetChannel(failing.ChannelSubscriptions.First().ChannelId)!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "50013");
        await fixture.GuildRepo.Save(failing);

        var result = await AdminDiscordEndpoints.GetSubscriptions(null, 1, 25, true, fixture.GuildRepo);

        var ok = Assert.IsType<Ok<PagedResult<GuildWithSubsDto>>>(result);
        Assert.Equal(failing.Id, Assert.Single(ok.Value!.Items).GuildId);
        Assert.DoesNotContain(ok.Value.Items, g => g.GuildId == healthy.Id);
    }

    [Fact]
    public async Task GetFailureStats_CountsFailingChannelsAndGuilds()
    {
        var failing = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = failing.ChannelSubscriptions.First().ChannelId;
        failing.GetChannel(channelId)!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "50013");
        await fixture.GuildRepo.Save(failing);

        var result = await AdminDiscordEndpoints.GetFailureStats(fixture.GuildRepo);

        var ok = Assert.IsType<Ok<ChannelFailureStatsDto>>(result);
        Assert.Equal(1, ok.Value!.ChannelsWithFailures);
        Assert.Equal(1, ok.Value.InstallationsWithFailures);
        Assert.Equal(0, ok.Value.ChannelsEligibleForPurge);
    }

    [Fact]
    public async Task ResetFailures_ClearsCountersAcrossAllGuilds()
    {
        var first = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var second = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        foreach (var guild in new[] { first, second })
        {
            guild.GetChannel(guild.ChannelSubscriptions.First().ChannelId)!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "50013");
            await fixture.GuildRepo.Save(guild);
        }

        await AdminDiscordEndpoints.ResetFailures(fixture.GuildRepo, NullLogger<Program>.Instance);

        foreach (var guild in new[] { first, second })
        {
            var reloaded = await fixture.GuildRepo.GetInstallation(guild.Id);
            var channel = reloaded.GetChannel(guild.ChannelSubscriptions.First().ChannelId)!;
            Assert.Equal(0, channel.FailureCount);
            Assert.Null(channel.FailingSince);
            Assert.Null(channel.LastFailureReason);
        }
    }

    [Fact]
    public async Task GetSubscriptions_FiltersByGuildId()
    {
        var matching = await fixture.SeedGuildInstallation();
        await fixture.SeedGuildInstallation();

        var result = await AdminDiscordEndpoints.GetSubscriptions(matching.Id, 1, 25, false, fixture.GuildRepo);

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

        var result = await AdminDiscordEndpoints.GetGuild(installedGuild.Id, fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient,
            NullLogger<Program>.Instance);

        var ok = Assert.IsAssignableFrom<IValueHttpResult>(result);
        dynamic value = ok.Value!;
        Assert.Equal(installedGuild.Id, (string)value.guildId);
    }

    [Fact]
    public async Task FollowLeague_SetsTheFollowedLeague()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        var leagueClient = A.Fake<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(999, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague { Properties = new ClassicLeagueProperties { Name = "New League" } });

        var result = await AdminDiscordEndpoints.FollowLeague(
            installedGuild.Id, channelId, new FollowGuildLeagueRequest(999), fixture.GuildRepo, leagueClient);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.Equal(999, (int)updated!.GetChannel(channelId)!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task FollowLeague_UnknownLeague_IsRejectedAndLeavesTheChannelUnchanged()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        var leagueClient = A.Fake<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(A<int>._, A<int>._, A<bool>._, A<int?>._)).Returns(Task.FromResult<ClassicLeague?>(null));

        var result = await AdminDiscordEndpoints.FollowLeague(
            installedGuild.Id, channelId, new FollowGuildLeagueRequest(404404), fixture.GuildRepo, leagueClient);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.Equal(123, (int)updated!.GetChannel(channelId)!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task UnfollowLeague_ClearsTheLeagueInRedis()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var result = await AdminDiscordEndpoints.UnfollowLeague(installedGuild.Id, channelId, fixture.GuildRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.Null(updated!.GetChannel(channelId)!.FollowedLeagueId);
    }

    [Fact]
    public async Task AddChannel_SubscribesTheChannelToAllEvents()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var result = await AdminDiscordEndpoints.AddChannel(
            installedGuild.Id, new AddGuildChannelRequest("222222222222222222"), fixture.GuildRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.True(updated!.GetChannel("222222222222222222")!.IsSubscribedTo(FplEvent.PriceChanges));
    }

    [Fact]
    public async Task AddChannel_AlreadySubscribed_ReturnsConflict()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var result = await AdminDiscordEndpoints.AddChannel(
            installedGuild.Id, new AddGuildChannelRequest(channelId), fixture.GuildRepo);

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
    }

    [Fact]
    public async Task MoveChannel_PublishesDiscordChannelMoved()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        var publisher = new TestPublishEndpoint();

        await AdminDiscordEndpoints.MoveChannel(installedGuild.Id, oldChannelId, new MoveGuildChannelRequest("222222222222222222"), fixture.GuildRepo,
            publisher);

        var moved = Assert.Single(publisher.PublishedMessages.Containing<DiscordChannelMoved>()).Message as DiscordChannelMoved;
        Assert.Equal(installedGuild.Id, moved!.GuildId);
        Assert.Equal(oldChannelId, moved.OldChannelId);
        Assert.Equal("222222222222222222", moved.NewChannelId);
    }

    [Fact]
    public async Task MoveChannel_TargetAlreadySubscribed_ReturnsConflictAndDoesNotPublish()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        installedGuild.Subscribe("222222222222222222", [FplEvent.Standings]);
        await fixture.GuildRepo.Save(installedGuild);
        var publisher = new TestPublishEndpoint();

        var result = await AdminDiscordEndpoints.MoveChannel(installedGuild.Id, oldChannelId, new MoveGuildChannelRequest("222222222222222222"),
            fixture.GuildRepo, publisher);

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.Empty(publisher.PublishedMessages.Containing<DiscordChannelMoved>());
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.NotNull(updated!.GetChannel(oldChannelId));
    }

    [Fact]
    public async Task GetAvailableChannels_ListsTextChannelsOnly()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        fixture.SetDiscordGuildChannels(
            new global::Discord.Net.HttpClients.DiscordClient.Channel(3, "zulu", 0),
            new global::Discord.Net.HttpClients.DiscordClient.Channel(1, "alpha", 0),
            new global::Discord.Net.HttpClients.DiscordClient.Channel(2, "a-category", 4),
            new global::Discord.Net.HttpClients.DiscordClient.Channel(4, "voice-chat", 2));
        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();

        var result = await AdminDiscordEndpoints.GetAvailableChannels(installedGuild.Id, fixture.GuildRepo, discordClient, NullLogger<Program>.Instance);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        var channels = ((IEnumerable<ChannelDto>)value).ToList();
        Assert.Equal(["alpha", "zulu"], channels.Select(c => c.Name));
        Assert.Equal("1", channels[0].Id);
    }

    [Fact]
    public async Task GetGuild_ResolvesChannelNameFromDiscord()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings], channelId: "893932860162064999");
        fixture.SetDiscordGuildChannels(new global::Discord.Net.HttpClients.DiscordClient.Channel(893932860162064999, "fplbot", 0));
        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();

        var result = await AdminDiscordEndpoints.GetGuild(installedGuild.Id, fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient,
            NullLogger<Program>.Instance);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        dynamic channel = Assert.Single((IEnumerable<object>)value.channels);
        Assert.Equal("fplbot", (string?)channel.channelName);
        Assert.True((bool?)channel.channelStatus);
    }

    [Fact]
    public async Task GetGuild_ChannelUnknownToDiscord_HasNoChannelName()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings], channelId: "893932860162064999");
        fixture.SetDiscordGuildChannels(new global::Discord.Net.HttpClients.DiscordClient.Channel(111111111111111111, "other", 0));
        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();

        var result = await AdminDiscordEndpoints.GetGuild(installedGuild.Id, fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient,
            NullLogger<Program>.Instance);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        dynamic channel = Assert.Single((IEnumerable<object>)value.channels);
        Assert.Null((string?)channel.channelName);
        Assert.False((bool?)channel.channelStatus);
    }

    [Fact]
    public async Task GetGuild_IncludesFailureState()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        var failingSince = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        installedGuild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince, "50013");
        installedGuild.GetChannel(channelId)!.RecordDeliveryFailure(failingSince.AddDays(1), "50013");
        await fixture.GuildRepo.Save(installedGuild);

        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();
        var result = await AdminDiscordEndpoints.GetGuild(installedGuild.Id, fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient,
            NullLogger<Program>.Instance);

        var ok = Assert.IsAssignableFrom<IValueHttpResult>(result);
        dynamic value = ok.Value!;
        dynamic channel = Assert.Single((IEnumerable<object>)value.channels);
        Assert.Equal(2, (int)channel.failureCount);
        Assert.Equal(failingSince, (DateTimeOffset?)channel.failingSince);
        Assert.Equal("50013", (string?)channel.lastFailureReason);
        Assert.Equal(failingSince + ChannelSubscription.MaxFailureAge, (DateTimeOffset?)channel.purgeEligibleAt);
        Assert.Equal(ChannelSubscription.MaxFailures - 2, (int)channel.failuresUntilPurge);
        Assert.Equal(ChannelSubscription.MaxFailures, (int)channel.purgeFailureLimit);
    }

    [Fact]
    public async Task GetGuild_GuildNotFound_ReturnsNotFound()
    {
        var discordClient = fixture.Services.GetRequiredService<global::Discord.Net.HttpClients.IDiscordClient>();

        var result = await AdminDiscordEndpoints.GetGuild(Guid.NewGuid().ToString("N"), fixture.GuildRepo, A.Fake<ILeagueClient>(), discordClient,
            NullLogger<Program>.Instance);

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
        var result = await AdminDiscordEndpoints.UpdateChannelSubscriptions(Guid.NewGuid().ToString("N"), "channel-1",
            new UpdateGuildChannelSubscriptionsRequest([]), fixture.GuildRepo);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task MoveChannel_MovesSubscriptionAndDeletesOldStorageKey()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var result = await AdminDiscordEndpoints.MoveChannel(installedGuild.Id, oldChannelId, new MoveGuildChannelRequest("new-channel"), fixture.GuildRepo,
            new TestPublishEndpoint());

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.Null(updated!.GetChannel(oldChannelId));
        Assert.Equal(123, (int)updated.GetChannel("new-channel")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task MoveChannel_ChannelNotFound_ReturnsNotFound()
    {
        var installedGuild = await fixture.SeedGuildInstallation();

        var result = await AdminDiscordEndpoints.MoveChannel(installedGuild.Id, "missing-channel", new MoveGuildChannelRequest("new-channel"),
            fixture.GuildRepo, new TestPublishEndpoint());

        Assert.IsType<NotFound>(result);
    }
}
