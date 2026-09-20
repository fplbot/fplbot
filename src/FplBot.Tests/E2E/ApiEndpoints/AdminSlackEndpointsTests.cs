using System.Net;
using System.Text.Json;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.PulseLive;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.Extensions.DependencyInjection;
using FakeItEasy;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Responses.ConversationsList;
using Response = Slackbot.Net.SlackClients.Http.Models.Responses.Response;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class AdminSlackEndpointsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        fixture.ResetChannelOutcomes();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Uninstall_SlackAcceptsUninstall_MarksForRemovalAndDeletesLocally()
    {
        var teamId = await fixture.InstallSlackbot();
        fixture.SetSlackAppsUninstallResult(new Response { Ok = true });

        var response = await fixture.Post($"/api/admin/teams/{await InstallationId(teamId)}/uninstall");

        response.EnsureSuccessStatusCode();
        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    [Fact]
    public async Task Uninstall_SlackRejectsUninstall_StillDeletesLocally()
    {
        var teamId = await fixture.InstallSlackbot();
        fixture.SetSlackAppsUninstallResult(new Response { Ok = false, Error = "something_broke" });

        await fixture.Post($"/api/admin/teams/{await InstallationId(teamId)}/uninstall");

        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    [Fact]
    public async Task Uninstall_SlackThrows_StillDeletesLocallyAndDoesNotCrash()
    {
        var teamId = await fixture.InstallSlackbot();
        fixture.SetSlackAppsUninstallThrows(new WellKnownSlackApiException(error: "account_inactive", responseContent: "{}"));

        await fixture.Post($"/api/admin/teams/{await InstallationId(teamId)}/uninstall");

        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    [Fact]
    public async Task GetTeams_FiltersByNameOrId_CaseInsensitive()
    {
        await fixture.InstallSlackbot("T1", "Blank");
        await fixture.InstallSlackbot("T2", "Other Workspace");
        await fixture.InstallSlackbot("T3", "Another blank one");

        var page = await fixture.GetJson<PagedResult<TeamSummaryDto>>("/api/admin/teams?query=blank");

        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, t => Assert.Contains("blank", (t.TeamId + t.TeamName).ToLowerInvariant()));
    }

    [Fact]
    public async Task GetTeams_WithoutPagingParameters_DefaultsToTheFirstPage()
    {
        await fixture.InstallSlackbot();

        var page = await fixture.GetJson<PagedResult<TeamSummaryDto>>("/api/admin/teams");

        Assert.Equal(1, page.Page);
        Assert.Equal(25, page.PageSize);
    }

    [Fact]
    public async Task GetTeams_Paginates()
    {
        foreach (var i in Enumerable.Range(1, 5))
        {
            await fixture.InstallSlackbot($"T{i}", $"Team {i}");
        }

        var page1 = await fixture.GetJson<PagedResult<TeamSummaryDto>>("/api/admin/teams?page=1&pageSize=2");
        var page2 = await fixture.GetJson<PagedResult<TeamSummaryDto>>("/api/admin/teams?page=2&pageSize=2");

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.NotEqual(page1.Items[0].TeamId, page2.Items[0].TeamId);
    }

    [Fact]
    public async Task Publish_Standings_ChannelNotFollowingLeague_DoesNotPublish()
    {
        await fixture.InstallSlackbot("T1", "Blank");
        await fixture.Subscribe("T1", "C0FPLBOT01", FplEvent.Standings);

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T1", "C0FPLBOT01")}/publish/Standings");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage());
    }

    [Fact]
    public async Task Publish_Standings_ChannelFollowingLeague_PublishesNow()
    {
        var teamId = await fixture.InstallSlackbot("T2", "Blank");
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        var subscriptionId = await SubscriptionId(teamId, "C0FPLBOT01");
        await fixture.Put($"/api/admin/subscriptions/{subscriptionId}/league", new FollowLeagueRequest(123));

        var response = await fixture.Post($"/api/admin/subscriptions/{subscriptionId}/publish/Standings");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        await fixture.SlackCapture.WaitForMessageAsync("C0FPLBOT01");
    }

    [Fact]
    public async Task Publish_GameweekStarted_ChannelFollowingLeague_PublishesToOnlyThatChannel()
    {
        var teamId = await fixture.InstallSlackbot("T3", "Blank");
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Captains);
        await fixture.Subscribe(teamId, "C0FPLBOT02", FplEvent.Captains);
        var subscriptionId = await SubscriptionId(teamId, "C0FPLBOT01");
        await fixture.Put($"/api/admin/subscriptions/{subscriptionId}/league", new FollowLeagueRequest(123));

        var response = await fixture.Post($"/api/admin/subscriptions/{subscriptionId}/publish/GameweekStarted");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        await fixture.SlackCapture.WaitForMessageAsync("C0FPLBOT01");
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage("C0FPLBOT02"));
    }

    [Fact]
    public async Task Publish_Deadline24Hours_ChannelNotFollowingLeague_StillPublishes()
    {
        await fixture.InstallSlackbot("T4", "Blank");
        await fixture.Subscribe("T4", "C0FPLBOT01", FplEvent.Deadlines);

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T4", "C0FPLBOT01")}/publish/Deadline24Hours");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.SlackCapture.WaitForMessageAsync("C0FPLBOT01");
        Assert.Contains("24 hours", msg.Text);
    }

    [Fact]
    public async Task Publish_Deadline1Hour_ChannelNotFollowingLeague_StillPublishes()
    {
        await fixture.InstallSlackbot("T4b", "Blank");
        await fixture.Subscribe("T4b", "C0FPLBOT01", FplEvent.Deadlines);

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T4b", "C0FPLBOT01")}/publish/Deadline1Hour");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.SlackCapture.WaitForMessageAsync("C0FPLBOT01");
        Assert.Contains("60 minutes", msg.Text);
    }

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

    [Fact]
    public async Task Publish_FixtureEvents_NoFixturesForGameweek_DoesNotPublish()
    {
        await fixture.InstallSlackbot("T6", "Blank");
        await fixture.Subscribe("T6", "C0FPLBOT01", FplEvent.FixtureGoals);
        A.CallTo(() => fixture.Services.GetRequiredService<IFixtureClient>().GetFixturesByGameweek(CurrentGameweekId)).Returns([]);

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T6", "C0FPLBOT01")}/publish/FixtureEvents");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage());
    }

    [Fact]
    public async Task Publish_FixtureEvents_RealGoalRecorded_PublishesIt()
    {
        await fixture.InstallSlackbot("T7", "Blank");
        await fixture.Subscribe("T7", "C0FPLBOT01", FplEvent.FixtureGoals);
        SeedFixture(RealPlayedFixture());

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T7", "C0FPLBOT01")}/publish/FixtureEvents");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.SlackCapture.WaitForMessageAsync("C0FPLBOT01");
        Assert.Contains("Raya", msg.Text);
    }

    [Fact]
    public async Task Publish_FixtureFullTime_FixtureFinished_PublishesScore()
    {
        await fixture.InstallSlackbot("T8", "Blank");
        await fixture.Subscribe("T8", "C0FPLBOT01", FplEvent.FixtureFullTime);
        SeedFixture(RealPlayedFixture(finished: true));

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T8", "C0FPLBOT01")}/publish/FixtureFullTime");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.SlackCapture.WaitForMessageAsync("C0FPLBOT01");
        Assert.Contains("2-1", msg.Text);
    }

    [Fact]
    public async Task Publish_Lineups_Confirmed_PublishesThem()
    {
        await fixture.InstallSlackbot("T9", "Blank");
        await fixture.Subscribe("T9", "C0FPLBOT01", FplEvent.Lineups);
        SeedFixture(RealPlayedFixture());
        SeedLineups();

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T9", "C0FPLBOT01")}/publish/Lineups");

        var value = await AppFixture.ReadJson<JsonElement>(response);
        Assert.True(value.GetProperty("published").GetBoolean());
        var msg = await fixture.SlackCapture.WaitForMessageAsync("C0FPLBOT01");
        Assert.Contains("Lineups", msg.Text);
    }

    [Fact]
    public async Task Publish_UnknownEventName_ReturnsBadRequest()
    {
        await fixture.InstallSlackbot("T5", "Blank");
        await fixture.Subscribe("T5", "C0FPLBOT01", FplEvent.Standings);

        var response = await fixture.Post(
            $"/api/admin/subscriptions/{await SubscriptionId("T5", "C0FPLBOT01")}/publish/NotARealEvent");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_AddsAndRemovesToMatchRequestedSet()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings, FplEvent.Captains);

        var response = await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0FPLBOT01")}/subscriptions",
            new UpdateChannelSubscriptionsRequest([EventSubscription.Captains, EventSubscription.Deadlines]));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        var channel = updated.GetChannel("C0FPLBOT01")!;
        Assert.Equal(
            new[] { FplEvent.Captains, FplEvent.Deadlines }.OrderBy(e => e),
            channel.Events.Current.OrderBy(e => e));
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_TeamNotFound_ReturnsNotFound()
    {
        var response = await fixture.Put(
            $"/api/admin/subscriptions/{Guid.NewGuid():N}/subscriptions",
            new UpdateChannelSubscriptionsRequest([]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MoveChannel_MovesSubscriptionAndDeletesOldStorageKey()
    {
        var teamId = await fixture.InstallSlackbot();
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        installation.Follow("C0OLD000001", new ClassicLeagueId(123));
        await fixture.SlackRepo.Save(installation);

        var response = await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0OLD000001")}/channel",
            new MoveChannelRequest("C0NEW000002"));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.Null(updated.GetChannel("C0OLD000001"));
        Assert.Equal(123, (int)updated.GetChannel("C0NEW000002")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task MoveChannel_KeepsTheSubscriptionAddressableByItsId()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0OLD000001", FplEvent.Standings);
        var before = (await fixture.SlackRepo.GetInstallation(teamId)).GetChannel("C0OLD000001")!;

        var response = await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0OLD000001")}/channel",
            new MoveChannelRequest("C0NEW000002"));

        response.EnsureSuccessStatusCode();
        var after = (await fixture.SlackRepo.GetInstallation(teamId)).GetChannel("C0NEW000002")!;
        Assert.Equal(before.Id, after.Id);

        var resolved = await fixture.Services.GetRequiredService<IIdentityResolver>().ResolveSubscription(before.Id);
        Assert.NotNull(resolved);
        Assert.Equal(teamId, resolved.InstallationExternalId);
        Assert.Equal("C0NEW000002", resolved.ChannelId);
    }

    [Fact]
    public async Task MoveChannel_KeepsDeliveryFailureState()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0OLD000001", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        installation.GetChannel("C0OLD000001")!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "channel_not_found");
        await fixture.SlackRepo.Save(installation);

        var response = await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0OLD000001")}/channel",
            new MoveChannelRequest("C0NEW000002"));

        response.EnsureSuccessStatusCode();
        var moved = (await fixture.SlackRepo.GetInstallation(teamId)).GetChannel("C0NEW000002")!;
        Assert.Equal(1, moved.FailureCount);
        Assert.Equal("channel_not_found", moved.LastFailureReason);
    }

    [Fact]
    public async Task MoveChannel_ChannelNotFound_ReturnsNotFound()
    {
        var teamId = await fixture.InstallSlackbot();

        var response = await fixture.Put($"/api/admin/subscriptions/{Guid.NewGuid():N}/channel",
            new MoveChannelRequest("C0NEW000002"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MoveChannel_TargetAlreadySubscribed_ReturnsConflictAndLeavesBothChannels()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0OLD000001", FplEvent.Standings);
        await fixture.Subscribe(teamId, "C0TAKEN0001", FplEvent.Standings);

        var response = await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0OLD000001")}/channel",
            new MoveChannelRequest("C0TAKEN0001"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.NotNull(updated.GetChannel("C0OLD000001"));
    }

    [Fact]
    public async Task FollowLeague_SetsTheFollowedLeague()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        LeagueExists(999, "New League");

        var response = await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0FPLBOT01")}/league",
            new FollowLeagueRequest(999));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.Equal(999, (int)updated.GetChannel("C0FPLBOT01")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task FollowLeague_UnknownLeague_IsRejectedAndLeavesTheChannelUnchanged()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        LeagueDoesNotExist(404404);

        var response = await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0FPLBOT01")}/league",
            new FollowLeagueRequest(404404));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.Null(updated.GetChannel("C0FPLBOT01")!.FollowedLeagueId);
    }

    [Fact]
    public async Task UnfollowLeague_ClearsTheLeagueInRedis()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        LeagueExists(999, "New League");
        await fixture.Put($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0FPLBOT01")}/league", new FollowLeagueRequest(999));

        var response = await fixture.Delete($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0FPLBOT01")}/league");

        response.EnsureSuccessStatusCode();
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.Null(updated.GetChannel("C0FPLBOT01")!.FollowedLeagueId);
    }

    [Fact]
    public async Task AddChannel_SubscribesTheChannelToAllEvents()
    {
        var teamId = await fixture.InstallSlackbot();

        var response = await fixture.Post($"/api/admin/teams/{await InstallationId(teamId)}/channels", new AddChannelRequest("C0NEW00001"));

        response.EnsureSuccessStatusCode();
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.True(updated.GetChannel("C0NEW00001")!.IsSubscribedTo(FplEvent.PriceChanges));
    }

    [Fact]
    public async Task AddChannel_AlreadySubscribed_ReturnsConflict()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);

        var response = await fixture.Post($"/api/admin/teams/{await InstallationId(teamId)}/channels", new AddChannelRequest("C0FPLBOT01"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableChannels_ListsChannelsFromSlack()
    {
        var teamId = await fixture.InstallSlackbot();
        fixture.SetSlackChannels(
            new Conversation { Id = "C0ZULU00001", Name = "zulu", Is_Channel = true },
            new Conversation { Id = "C0ALPHA0001", Name = "alpha", Is_Channel = true });

        var channels = await fixture.GetJson<JsonElement>($"/api/admin/teams/{await InstallationId(teamId)}/available-channels");

        var listed = channels.EnumerateArray().ToList();
        Assert.Equal(["alpha", "zulu"], listed.Select(c => c.GetProperty("name").GetString()));
        Assert.Equal("C0ALPHA0001", listed[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetTeam_ResolvesChannelNameFromSlack()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        fixture.SetSlackChannels(new Conversation { Id = "C0FPLBOT01", Name = "fplbot", Is_Channel = true });

        var team = await fixture.GetJson<JsonElement>($"/api/admin/teams/{await InstallationId(teamId)}");

        var channel = Assert.Single(team.GetProperty("channels").EnumerateArray());
        Assert.Equal("fplbot", channel.GetProperty("channelName").GetString());
        Assert.True(channel.GetProperty("channelStatus").GetBoolean());
    }

    [Fact]
    public async Task GetTeam_ChannelUnknownToSlack_HasNoChannelName()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0GONE0001", FplEvent.Standings);
        fixture.SetSlackChannels(new Conversation { Id = "C0FPLBOT01", Name = "fplbot", Is_Channel = true });

        var team = await fixture.GetJson<JsonElement>($"/api/admin/teams/{await InstallationId(teamId)}");

        var channel = Assert.Single(team.GetProperty("channels").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, channel.GetProperty("channelName").ValueKind);
        Assert.False(channel.GetProperty("channelStatus").GetBoolean());
    }

    [Fact]
    public async Task GetTeam_IncludesTheIdsTheAdminUiLinksWith()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);

        var team = await fixture.GetJson<JsonElement>($"/api/admin/teams/{await InstallationId(teamId)}");

        Assert.Equal(installation.Id.Value, team.GetProperty("id").GetString());
        var channel = Assert.Single(team.GetProperty("channels").EnumerateArray());
        Assert.Equal(installation.GetChannel("C0FPLBOT01")!.Id.Value, channel.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetTeam_IncludesFailureState()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        var failingSince = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        installation.GetChannel("C0FPLBOT01")!.RecordDeliveryFailure(failingSince, "not_in_channel");
        installation.GetChannel("C0FPLBOT01")!.RecordDeliveryFailure(failingSince.AddDays(1), "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var team = await fixture.GetJson<JsonElement>($"/api/admin/teams/{await InstallationId(teamId)}");

        var channel = Assert.Single(team.GetProperty("channels").EnumerateArray());
        Assert.Equal(2, channel.GetProperty("failureCount").GetInt32());
        Assert.Equal(failingSince, channel.GetProperty("failingSince").GetDateTimeOffset());
        Assert.Equal("not_in_channel", channel.GetProperty("lastFailureReason").GetString());
        Assert.Equal(failingSince + ChannelSubscription.MaxFailureAge, channel.GetProperty("purgeEligibleAt").GetDateTimeOffset());
        Assert.Equal(ChannelSubscription.MaxFailures - 2, channel.GetProperty("failuresUntilPurge").GetInt32());
        Assert.Equal(ChannelSubscription.MaxFailures, channel.GetProperty("purgeFailureLimit").GetInt32());
    }

    [Fact]
    public async Task GetTeams_FailingOnly_ExcludesHealthyTeams()
    {
        var failingTeamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(failingTeamId, "C0FPLBOT01", FplEvent.Standings);
        var healthyTeamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(healthyTeamId, "C0FPLBOT01", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(failingTeamId);
        installation.GetChannel("C0FPLBOT01")!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var page = await fixture.GetJson<PagedResult<TeamSummaryDto>>("/api/admin/teams?failingOnly=true");

        Assert.Equal(failingTeamId, Assert.Single(page.Items).TeamId);
        Assert.DoesNotContain(page.Items, t => t.TeamId == healthyTeamId);
    }

    [Fact]
    public async Task GetFailureStats_CountsFailingChannelsAndTeams()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        installation.GetChannel("C0FPLBOT01")!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var stats = await fixture.GetJson<ChannelFailureStatsDto>("/api/admin/slack/failures");

        Assert.Equal(1, stats.ChannelsWithFailures);
        Assert.Equal(1, stats.InstallationsWithFailures);
        Assert.Equal(0, stats.ChannelsEligibleForPurge);
    }

    [Fact]
    public async Task ResetFailures_ClearsCountersAcrossAllTeams()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        installation.GetChannel("C0FPLBOT01")!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var response = await fixture.Post("/api/admin/slack/failures/reset");

        response.EnsureSuccessStatusCode();
        var reloaded = await fixture.SlackRepo.GetInstallation(teamId);
        var channel = reloaded.GetChannel("C0FPLBOT01")!;
        Assert.Equal(0, channel.FailureCount);
        Assert.Null(channel.FailingSince);
        Assert.Null(channel.LastFailureReason);
    }

    [Fact]
    public async Task DeleteChannelSubscription_RemovesSubscriptionFromRepository()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);

        var response = await fixture.Delete($"/api/admin/subscriptions/{await SubscriptionId(teamId, "C0FPLBOT01")}");

        response.EnsureSuccessStatusCode();
        var remaining = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.DoesNotContain(remaining.ChannelSubscriptions, c => c.ChannelId == "C0FPLBOT01");
    }

    private void LeagueExists(int leagueId, string name) =>
        A.CallTo(() => fixture.Services.GetRequiredService<ILeagueClient>()
                .GetClassicLeague(leagueId, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague { Properties = new ClassicLeagueProperties { Name = name } });

    private void LeagueDoesNotExist(int leagueId) =>
        A.CallTo(() => fixture.Services.GetRequiredService<ILeagueClient>()
                .GetClassicLeague(leagueId, A<int>._, A<bool>._, A<int?>._))
            .Returns(Task.FromResult<ClassicLeague?>(null));

    private async Task<string> InstallationId(string teamId) =>
        (await fixture.SlackRepo.GetInstallation(teamId)).Id.Value;

    private async Task<string> SubscriptionId(string teamId, string channelId) =>
        (await fixture.SlackRepo.GetInstallation(teamId)).GetChannel(channelId)!.Id.Value;

    private async Task<Installation?> WaitForInstallationToBeGone(string teamId)
    {
        var repository = fixture.Services.GetRequiredService<ISlackTeamRepository>();

        Installation? installation = null;
        try
        {
            await AppFixture.WaitUntil(async () => (installation = await repository.FindInstallationByTeamId(teamId)) is null);
        }
        catch (TimeoutException)
        {
        }

        return installation;
    }
}
