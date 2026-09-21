using System.Net;
using System.Text.Json;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.PulseLive;
using FplBot.Data;
using FplBot.Domain;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.Extensions.DependencyInjection;
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

    private static string SubId(Installation installation, string channelId) =>
        installation.GetChannel(channelId)!.Id.Value;

    [Fact]
    public async Task GetSubscriptions_ReturnsGuildsWithTheirChannelSubscriptions()
    {
        var installedGuild = await fixture.SeedGuildInstallation(12345, [EventSubscription.Standings]);

        var page = await fixture.GetJson<PagedResult<GuildWithSubsDto>>("/api/admin/discord/servers");

        var guild = Assert.Single(page.Items, g => g.GuildId == installedGuild.ExternalId);
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

        var page = await fixture.GetJson<PagedResult<GuildWithSubsDto>>("/api/admin/discord/servers");

        var dto = page.Items.Single(g => g.GuildId == guild.ExternalId).Subscriptions.Single();
        Assert.Equal(2, dto.FailureCount);
        Assert.Equal(failingSince, dto.FailingSince);
        Assert.Equal("50013", dto.LastFailureReason);
    }

    [Fact]
    public async Task GetSubscriptions_WithoutPagingParameters_DefaultsToTheFirstPage()
    {
        await fixture.SeedGuildInstallation();

        var page = await fixture.GetJson<PagedResult<GuildWithSubsDto>>("/api/admin/discord/servers");

        Assert.Equal(1, page.Page);
        Assert.Equal(25, page.PageSize);
    }

    [Fact]
    public async Task GetSubscriptions_FailingOnly_ExcludesHealthyGuilds()
    {
        var failing = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var healthy = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        failing.GetChannel(failing.ChannelSubscriptions.First().ChannelId)!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "50013");
        await fixture.GuildRepo.Save(failing);

        var page = await fixture.GetJson<PagedResult<GuildWithSubsDto>>("/api/admin/discord/servers?failingOnly=true");

        Assert.Equal(failing.ExternalId, Assert.Single(page.Items).GuildId);
        Assert.DoesNotContain(page.Items, g => g.GuildId == healthy.ExternalId);
    }

    [Fact]
    public async Task GetSubscriptions_FiltersByGuildId()
    {
        var matching = await fixture.SeedGuildInstallation();
        await fixture.SeedGuildInstallation();

        var page = await fixture.GetJson<PagedResult<GuildWithSubsDto>>($"/api/admin/discord/servers?query={matching.ExternalId}");

        Assert.Equal(matching.ExternalId, Assert.Single(page.Items).GuildId);
    }

    [Fact]
    public async Task GetSubscriptions_MinMembers_ExcludesSmallerGuilds()
    {
        var small = await fixture.SeedGuildInstallation();
        var big = await fixture.SeedGuildInstallation();
        await fixture.GuildMemberCountRepo.SetApproximateMemberCount(small.ExternalId, 5);
        await fixture.GuildMemberCountRepo.SetApproximateMemberCount(big.ExternalId, 500);

        var page = await fixture.GetJson<PagedResult<GuildWithSubsDto>>("/api/admin/discord/servers?minMembers=100");

        Assert.Equal(big.ExternalId, Assert.Single(page.Items).GuildId);
    }

    [Fact]
    public async Task GetReachStats_SumsApproximateMemberCountsAcrossGuilds()
    {
        var first = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var second = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        await fixture.GuildMemberCountRepo.SetApproximateMemberCount(first.ExternalId, 10);
        await fixture.GuildMemberCountRepo.SetApproximateMemberCount(second.ExternalId, 32);

        var stats = await fixture.GetJson<GuildReachStatsDto>("/api/admin/discord/reach");

        Assert.Equal(2, stats.TotalGuilds);
        Assert.Equal(42, stats.TotalApproximateMembers);
    }

    [Fact]
    public async Task GetReachStats_GuildWithNoStoredCountYet_ContributesZero()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var stats = await fixture.GetJson<GuildReachStatsDto>("/api/admin/discord/reach");

        Assert.Equal(1, stats.TotalGuilds);
        Assert.Equal(0, stats.TotalApproximateMembers);
    }

    [Fact]
    public async Task GetFailureStats_CountsFailingChannelsAndGuilds()
    {
        var failing = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = failing.ChannelSubscriptions.First().ChannelId;
        failing.GetChannel(channelId)!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "50013");
        await fixture.GuildRepo.Save(failing);

        var stats = await fixture.GetJson<ChannelFailureStatsDto>("/api/admin/discord/failures");

        Assert.Equal(1, stats.ChannelsWithFailures);
        Assert.Equal(1, stats.InstallationsWithFailures);
        Assert.Equal(0, stats.ChannelsEligibleForPurge);
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

        var response = await fixture.Post("/api/admin/discord/failures/reset");

        response.EnsureSuccessStatusCode();
        foreach (var guild in new[] { first, second })
        {
            var reloaded = await fixture.GuildRepo.GetInstallation(guild.ExternalId);
            var channel = reloaded.GetChannel(guild.ChannelSubscriptions.First().ChannelId)!;
            Assert.Equal(0, channel.FailureCount);
            Assert.Null(channel.FailingSince);
            Assert.Null(channel.LastFailureReason);
        }
    }

    [Fact]
    public async Task DeleteSubscription_RemovesJustThatChannel()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Delete($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}");

        response.EnsureSuccessStatusCode();
        var remaining = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.DoesNotContain(remaining!.ChannelSubscriptions, c => c.ChannelId == channelId);
    }

    [Fact]
    public async Task DeleteAllSubscriptionsForGuild_RemovesChannelsButKeepsGuildInstalled()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var response = await fixture.Delete($"/api/admin/discord/guilds/{installedGuild.Id.Value}/subscriptions");

        response.EnsureSuccessStatusCode();
        var remaining = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.NotNull(remaining);
        Assert.Empty(remaining.ChannelSubscriptions);
    }

    [Fact]
    public async Task DeleteGuild_RemovesGuildEntirely()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var response = await fixture.Delete($"/api/admin/discord/guilds/{installedGuild.Id.Value}");

        response.EnsureSuccessStatusCode();
        Assert.Null(await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId));
        Assert.True(fixture.DiscordCapture.LeftGuild(installedGuild.ExternalId));
    }

    [Fact]
    public async Task DeleteGuild_WhenTheBotIsAlreadyGone_StillRemovesGuild()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        fixture.DiscordChannelFails(installedGuild.ExternalId, HttpStatusCode.NotFound);

        var response = await fixture.Delete($"/api/admin/discord/guilds/{installedGuild.Id.Value}");

        response.EnsureSuccessStatusCode();
        Assert.Null(await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId));
    }

    [Fact]
    public async Task GetGuild_ReturnsGuildNameAndChannels()
    {
        var installedGuild = await fixture.SeedGuildInstallation(12345, [EventSubscription.Standings]);

        var guild = await fixture.GetJson<JsonElement>($"/api/admin/discord/guilds/{installedGuild.Id.Value}");

        Assert.Equal(installedGuild.ExternalId, guild.GetProperty("guildId").GetString());
    }

    [Fact]
    public async Task GetGuild_ResolvesChannelNameFromDiscord()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings], channelId: "893932860162064999");
        fixture.SetDiscordGuildChannels(new global::Discord.Net.HttpClients.DiscordClient.Channel(893932860162064999, "fplbot", 0));

        var guild = await fixture.GetJson<JsonElement>($"/api/admin/discord/guilds/{installedGuild.Id.Value}");

        var channel = Assert.Single(guild.GetProperty("channels").EnumerateArray());
        Assert.Equal("fplbot", channel.GetProperty("channelName").GetString());
        Assert.True(channel.GetProperty("channelStatus").GetBoolean());
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

        var channels = await fixture.GetJson<JsonElement>($"/api/admin/discord/guilds/{installedGuild.Id.Value}/available-channels");

        var listed = channels.EnumerateArray().ToList();
        Assert.Equal(["alpha", "zulu"], listed.Select(c => c.GetProperty("name").GetString()));
        Assert.Equal("1", listed[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task FollowLeague_SetsTheFollowedLeague()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        LeagueExists(999, "New League");

        var response = await fixture.Put(
            $"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/league", new FollowLeagueRequest(999));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Equal(999, (int)updated!.GetChannel(channelId)!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task FollowLeague_UnknownLeague_IsRejectedAndLeavesTheChannelUnchanged()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        LeagueDoesNotExist(404404);

        var response = await fixture.Put(
            $"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/league", new FollowLeagueRequest(404404));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Equal(123, (int)updated!.GetChannel(channelId)!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task UnfollowLeague_ClearsTheLeagueInRedis()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Delete($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/league");

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Null(updated!.GetChannel(channelId)!.FollowedLeagueId);
    }

    [Fact]
    public async Task AddChannel_SubscribesTheChannelToAllEvents()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var response = await fixture.Post(
            $"/api/admin/discord/guilds/{installedGuild.Id.Value}/channels", new AddChannelRequest("222222222222222222"));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.True(updated!.GetChannel("222222222222222222")!.IsSubscribedTo(FplEvent.PriceChanges));
    }

    [Fact]
    public async Task AddChannel_AlreadySubscribed_ReturnsConflict()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post(
            $"/api/admin/discord/guilds/{installedGuild.Id.Value}/channels", new AddChannelRequest(channelId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task MoveChannel_MovesTheSubscriptionToTheNewChannel()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Put(
            $"/api/admin/subscriptions/{SubId(installedGuild, oldChannelId)}/channel",
            new MoveChannelRequest("222222222222222222"));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Null(updated!.GetChannel(oldChannelId));
        Assert.NotNull(updated.GetChannel("222222222222222222"));
    }

    [Fact]
    public async Task MoveChannel_TargetAlreadySubscribed_ReturnsConflictAndLeavesBothChannels()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        installedGuild.Subscribe("222222222222222222", [FplEvent.Standings]);
        await fixture.GuildRepo.Save(installedGuild);

        var response = await fixture.Put(
            $"/api/admin/subscriptions/{SubId(installedGuild, oldChannelId)}/channel",
            new MoveChannelRequest("222222222222222222"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.NotNull(updated!.GetChannel(oldChannelId));
    }

    [Fact]
    public async Task GetGuild_ChannelUnknownToDiscord_HasNoChannelName()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings], channelId: "893932860162064999");
        fixture.SetDiscordGuildChannels(new global::Discord.Net.HttpClients.DiscordClient.Channel(111111111111111111, "other", 0));

        var guild = await fixture.GetJson<JsonElement>($"/api/admin/discord/guilds/{installedGuild.Id.Value}");

        var channel = Assert.Single(guild.GetProperty("channels").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, channel.GetProperty("channelName").ValueKind);
        Assert.False(channel.GetProperty("channelStatus").GetBoolean());
    }

    [Fact]
    public async Task GetGuild_IncludesTheIdsTheAdminUiLinksWith()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var subscription = installedGuild.ChannelSubscriptions.First();

        var guild = await fixture.GetJson<JsonElement>($"/api/admin/discord/guilds/{installedGuild.Id.Value}");

        Assert.Equal(installedGuild.Id.Value, guild.GetProperty("id").GetString());
        var channel = Assert.Single(guild.GetProperty("channels").EnumerateArray());
        Assert.Equal(subscription.Id.Value, channel.GetProperty("id").GetString());
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

        var guild = await fixture.GetJson<JsonElement>($"/api/admin/discord/guilds/{installedGuild.Id.Value}");

        var channel = Assert.Single(guild.GetProperty("channels").EnumerateArray());
        Assert.Equal(2, channel.GetProperty("failureCount").GetInt32());
        Assert.Equal(failingSince, channel.GetProperty("failingSince").GetDateTimeOffset());
        Assert.Equal("50013", channel.GetProperty("lastFailureReason").GetString());
        Assert.Equal(failingSince + ChannelSubscription.MaxFailureAge, channel.GetProperty("purgeEligibleAt").GetDateTimeOffset());
        Assert.Equal(ChannelSubscription.MaxFailures - 2, channel.GetProperty("failuresUntilPurge").GetInt32());
        Assert.Equal(ChannelSubscription.MaxFailures, channel.GetProperty("purgeFailureLimit").GetInt32());
    }

    [Fact]
    public async Task GetGuild_GuildNotFound_ReturnsNotFound()
    {
        var response = await fixture.Get($"/api/admin/discord/guilds/{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_Standings_ChannelNotFollowingLeague_DoesNotPublish()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: null, subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/Standings");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }

    [Fact]
    public async Task Publish_Standings_ChannelFollowingLeague_PublishesNow()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/Standings");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        await fixture.DiscordCapture.WaitForMessageAsync(channelId);
    }

    [Fact]
    public async Task Publish_GameweekStarted_ChannelFollowingLeague_PublishesToOnlyThatChannel()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Captains],
            channelId: "channel-a");
        var otherGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Captains],
            channelId: "channel-b");
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/GameweekStarted");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(otherGuild.ChannelSubscriptions.First().ChannelId));
    }

    [Fact]
    public async Task Publish_Deadline24Hours_ChannelNotFollowingLeague_StillPublishes()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: null, subscriptions: [EventSubscription.Deadlines]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/Deadline24Hours");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("24 hours", msg.Text);
    }

    [Fact]
    public async Task Publish_Deadline1Hour_ChannelNotFollowingLeague_StillPublishes()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: null, subscriptions: [EventSubscription.Deadlines]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/Deadline1Hour");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("60 minutes", msg.Text);
    }

    [Fact]
    public async Task Publish_UnknownEventName_ReturnsBadRequest()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/NotARealEvent");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_AddsAndRemovesToMatchRequestedSet()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings, EventSubscription.Captains]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Put($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/subscriptions",
            new UpdateChannelSubscriptionsRequest([EventSubscription.Captains, EventSubscription.Deadlines]));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        var channel = updated!.GetChannel(channelId)!;
        Assert.Equal(
            new[] { FplEvent.Captains, FplEvent.Deadlines }.OrderBy(e => e),
            channel.Events.Current.OrderBy(e => e));
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_GuildNotFound_ReturnsNotFound()
    {
        var response = await fixture.Put($"/api/admin/subscriptions/{Guid.NewGuid():N}/subscriptions",
            new UpdateChannelSubscriptionsRequest([]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MoveChannel_MovesSubscriptionAndDeletesOldStorageKey()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Put($"/api/admin/subscriptions/{SubId(installedGuild, oldChannelId)}/channel",
            new MoveChannelRequest("new-channel"));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Null(updated!.GetChannel(oldChannelId));
        Assert.Equal(123, (int)updated.GetChannel("new-channel")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task MoveChannel_ChannelNotFound_ReturnsNotFound()
    {
        var installedGuild = await fixture.SeedGuildInstallation();

        var response = await fixture.Put($"/api/admin/subscriptions/{Guid.NewGuid():N}/channel",
            new MoveChannelRequest("new-channel"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private void LeagueExists(int leagueId, string name) =>
        A.CallTo(() => fixture.Services.GetRequiredService<ILeagueClient>()
                .GetClassicLeague(leagueId, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague { Properties = new ClassicLeagueProperties { Name = name } });

    private void LeagueDoesNotExist(int leagueId) =>
        A.CallTo(() => fixture.Services.GetRequiredService<ILeagueClient>()
                .GetClassicLeague(leagueId, A<int>._, A<bool>._, A<int?>._))
            .Returns(Task.FromResult<ClassicLeague?>(null));

    // The current gameweek in bootstrap-static.json (see GameweekExtensions.GetCurrentGameweek).
    private const int CurrentGameweekId = 3;

    private static Fixture RealPlayedFixture(bool finished = false) => new()
    {
        Id = 1,
        Code = 1,
        Event = CurrentGameweekId,
        HomeTeamId = 1,
        AwayTeamId = 2,
        KickOffTime = DateTime.UtcNow.AddHours(-2),
        Minutes = 90,
        Finished = finished,
        FinishedProvisional = finished,
        HomeTeamScore = finished ? 2 : 1,
        AwayTeamScore = finished ? 1 : 0,
        Stats =
        [
            new FixtureStat
            {
                Identifier = "goals_scored",
                HomeStats = [new FixtureStatValue { Element = 1, Value = finished ? 2 : 1 }],
                AwayStats = finished ? [new FixtureStatValue { Element = 2, Value = 1 }] : []
            }
        ]
    };

    private void SeedFixture(Fixture fixture_) =>
        A.CallTo(() => fixture.Services.GetRequiredService<IFixtureClient>().GetFixturesByGameweek(CurrentGameweekId))
            .Returns([fixture_]);

    private void SeedNoFixtures() =>
        A.CallTo(() => fixture.Services.GetRequiredService<IFixtureClient>().GetFixturesByGameweek(CurrentGameweekId))
            .Returns([]);

    private void SeedLineups()
    {
        var homeLineup = new TeamLineup
        {
            TeamId = 1,
            Players = [new PulsePlayer { Id = 1, KnownName = "Raya", Position = "Goalkeeper", IsCaptain = false }],
            Formation = new PulseFormation { Label = "4-3-3", Lineup = [[1]] }
        };
        var awayLineup = new TeamLineup
        {
            TeamId = 2,
            Players = [new PulsePlayer { Id = 2, KnownName = "Martinez", Position = "Goalkeeper", IsCaptain = false }],
            Formation = new PulseFormation { Label = "4-4-2", Lineup = [[2]] }
        };
        A.CallTo(() => fixture.Services.GetRequiredService<IPulseLiveClient>().GetMatchDetails(1))
            .Returns(new MatchDetails { HomeTeam = homeLineup, AwayTeam = awayLineup });
    }

    // Other tests in this shared-fixture collection may configure GetMatchDetails(1) too (fixture
    // code 1 is reused across these publish-now tests), so "not confirmed yet" must stub its own
    // null result rather than relying on an unconfigured fake's default.
    private void SeedNoLineups() =>
        A.CallTo(() => fixture.Services.GetRequiredService<IPulseLiveClient>().GetMatchDetails(1))
            .Returns((MatchDetails?)null);

    [Fact]
    public async Task Publish_FixtureEvents_NoFixturesForGameweek_DoesNotPublish()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.FixtureGoals]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        SeedNoFixtures();

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/FixtureEvents");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }

    [Fact]
    public async Task Publish_FixtureEvents_RealGoalRecorded_PublishesIt()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.FixtureGoals]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        SeedFixture(RealPlayedFixture());

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/FixtureEvents");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Raya", msg.Description);
    }

    [Fact]
    public async Task Publish_FixtureFullTime_FixtureNotFinished_DoesNotPublish()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.FixtureFullTime]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        SeedFixture(RealPlayedFixture(finished: false));

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/FixtureFullTime");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }

    [Fact]
    public async Task Publish_FixtureFullTime_FixtureFinished_PublishesScore()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.FixtureFullTime]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        SeedFixture(RealPlayedFixture(finished: true));

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/FixtureFullTime");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("2-1", msg.Title);
    }

    [Fact]
    public async Task Publish_Lineups_NotConfirmedYet_DoesNotPublish()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Lineups]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        SeedFixture(RealPlayedFixture());
        SeedNoLineups();

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/Lineups");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }

    [Fact]
    public async Task Publish_Lineups_Confirmed_PublishesThem()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Lineups]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        SeedFixture(RealPlayedFixture());
        SeedLineups();

        var response = await fixture.Post($"/api/admin/subscriptions/{SubId(installedGuild, channelId)}/publish/Lineups");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Lineups", msg.Title);
    }
}
