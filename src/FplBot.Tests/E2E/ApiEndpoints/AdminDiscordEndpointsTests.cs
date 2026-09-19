using System.Net;
using System.Text.Json;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
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

        var response = await fixture.Delete($"/api/admin/discord/subscriptions/{SubId(installedGuild, channelId)}");

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
            $"/api/admin/discord/subscriptions/{SubId(installedGuild, channelId)}/league", new FollowGuildLeagueRequest(999));

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
            $"/api/admin/discord/subscriptions/{SubId(installedGuild, channelId)}/league", new FollowGuildLeagueRequest(404404));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Equal(123, (int)updated!.GetChannel(channelId)!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task UnfollowLeague_ClearsTheLeagueInRedis()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Delete($"/api/admin/discord/subscriptions/{SubId(installedGuild, channelId)}/league");

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Null(updated!.GetChannel(channelId)!.FollowedLeagueId);
    }

    [Fact]
    public async Task AddChannel_SubscribesTheChannelToAllEvents()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);

        var response = await fixture.Post(
            $"/api/admin/discord/guilds/{installedGuild.Id.Value}/channels", new AddGuildChannelRequest("222222222222222222"));

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
            $"/api/admin/discord/guilds/{installedGuild.Id.Value}/channels", new AddGuildChannelRequest(channelId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task MoveChannel_MovesTheSubscriptionToTheNewChannel()
    {
        var installedGuild = await fixture.SeedGuildInstallation(123, [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Put(
            $"/api/admin/discord/subscriptions/{SubId(installedGuild, oldChannelId)}/channel",
            new MoveGuildChannelRequest("222222222222222222"));

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
            $"/api/admin/discord/subscriptions/{SubId(installedGuild, oldChannelId)}/channel",
            new MoveGuildChannelRequest("222222222222222222"));

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
    public async Task PublishStandings_ChannelNotFollowingLeague_DoesNotPublish()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: null, subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Post($"/api/admin/discord/subscriptions/{SubId(installedGuild, channelId)}/publish-standings");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_AddsAndRemovesToMatchRequestedSet()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings, EventSubscription.Captains]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Put($"/api/admin/discord/subscriptions/{SubId(installedGuild, channelId)}/subscriptions",
            new UpdateGuildChannelSubscriptionsRequest([EventSubscription.Captains, EventSubscription.Deadlines]));

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
        var response = await fixture.Put($"/api/admin/discord/subscriptions/{Guid.NewGuid():N}/subscriptions",
            new UpdateGuildChannelSubscriptionsRequest([]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MoveChannel_MovesSubscriptionAndDeletesOldStorageKey()
    {
        var installedGuild = await fixture.SeedGuildInstallation(leagueId: 123, subscriptions: [EventSubscription.Standings]);
        var oldChannelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.Put($"/api/admin/discord/subscriptions/{SubId(installedGuild, oldChannelId)}/channel",
            new MoveGuildChannelRequest("new-channel"));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.Null(updated!.GetChannel(oldChannelId));
        Assert.Equal(123, (int)updated.GetChannel("new-channel")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task MoveChannel_ChannelNotFound_ReturnsNotFound()
    {
        var installedGuild = await fixture.SeedGuildInstallation();

        var response = await fixture.Put($"/api/admin/discord/subscriptions/{Guid.NewGuid():N}/channel",
            new MoveGuildChannelRequest("new-channel"));

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
}
