using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.ApplicationServices.Slack;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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

        var result = await ExecuteUninstall(teamId);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    [Fact]
    public async Task Uninstall_SlackRejectsUninstall_StillDeletesLocally()
    {
        var teamId = await fixture.InstallSlackbot();
        fixture.SetSlackAppsUninstallResult(new Response { Ok = false, Error = "something_broke" });

        await ExecuteUninstall(teamId);

        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    [Fact]
    public async Task Uninstall_SlackThrows_StillDeletesLocallyAndDoesNotCrash()
    {
        var teamId = await fixture.InstallSlackbot();
        fixture.SetSlackAppsUninstallThrows(new WellKnownSlackApiException(error: "account_inactive", responseContent: "{}"));

        await ExecuteUninstall(teamId);

        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    private async Task<IResult> ExecuteUninstall(string teamId)
    {
        using var scope = fixture.Services.CreateScope();
        return await AdminSlackEndpoints.Uninstall(teamId, scope.ServiceProvider.GetRequiredService<AdminUninstallSlackWorkspace>(),
            NullLogger<Program>.Instance);
    }

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

    private static Installation Team(string id, string name) => Installation.Load(id, name, "token", []);

    private static ISlackTeamRepository RepoWithTeams(params Installation[] teams)
    {
        var repo = A.Fake<ISlackTeamRepository>();

        A.CallTo(() => repo.GetAllInstallations()).Returns(Task.FromResult(teams.AsEnumerable()));
        return repo;
    }

    [Fact]
    public async Task GetTeams_FiltersByNameOrId_CaseInsensitive()
    {
        var repo = RepoWithTeams(
            Team("T1", "Blank"),
            Team("T2", "Other Workspace"),
            Team("T3", "Another blank one"));

        var result = await AdminSlackEndpoints.GetTeams("blank", 1, 25, false, repo);

        var ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(result);
        Assert.Equal(2, ok.Value!.TotalCount);
        Assert.All(ok.Value.Items, t => Assert.Contains("blank", (t.TeamId + t.TeamName).ToLowerInvariant()));
    }

    [Fact]
    public async Task GetTeams_Paginates()
    {
        var teams = Enumerable.Range(1, 5).Select(i => Team($"T{i}", $"Team {i}")).ToArray();
        var repo = RepoWithTeams(teams);

        var page1 = await AdminSlackEndpoints.GetTeams(null, 1, 2, false, repo);
        var page2 = await AdminSlackEndpoints.GetTeams(null, 2, 2, false, repo);

        var page1Ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(page1);
        var page2Ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(page2);
        Assert.Equal(5, page1Ok.Value!.TotalCount);
        Assert.Equal(2, page1Ok.Value.Items.Count);
        Assert.Equal(2, page2Ok.Value!.Items.Count);
        Assert.NotEqual(page1Ok.Value.Items[0].TeamId, page2Ok.Value.Items[0].TeamId);
    }

    [Fact]
    public async Task PublishStandings_ChannelNotFollowingLeague_DoesNotPublish()
    {
        await fixture.InstallSlackbot("T1", "Blank");
        await fixture.Subscribe("T1", "#fplbot", FplEvent.Standings);

        var result = await AdminSlackEndpoints.PublishStandings(
            "T1", "#fplbot", fixture.SlackRepo, fixture.Publisher, A.Fake<IGlobalSettingsClient>());

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        Assert.False((bool)value.published);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage());
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_AddsAndRemovesToMatchRequestedSet()
    {
        var teamId = await fixture.InstallSlackbot();

        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings, FplEvent.Captains);

        var request = new UpdateChannelSubscriptionsRequest([EventSubscription.Captains, EventSubscription.Deadlines]);
        var result = await AdminSlackEndpoints.UpdateChannelSubscriptions(teamId, "#fplbot", request, fixture.SlackRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        var channel = updated.GetChannel("#fplbot")!;
        Assert.Equal(
            new[] { FplEvent.Captains, FplEvent.Deadlines }.OrderBy(e => e),
            channel.Events.Current.OrderBy(e => e));
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_TeamNotFound_ReturnsNotFound()
    {
        var repo = fixture.Services.GetRequiredService<ISlackTeamRepository>();

        var result = await AdminSlackEndpoints.UpdateChannelSubscriptions(Guid.NewGuid().ToString("N"), "#fplbot", new UpdateChannelSubscriptionsRequest([]),
            repo);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task MoveChannel_MovesSubscriptionAndDeletesOldStorageKey()
    {
        var teamId = await fixture.InstallSlackbot();
        var repo = fixture.Services.GetRequiredService<ISlackTeamRepository>();
        var installation = await repo.GetInstallation(teamId);
        installation.Follow("#old-channel", new ClassicLeagueId(123));
        await repo.Save(installation);

        var result = await AdminSlackEndpoints.MoveChannel(teamId, "#old-channel", new MoveChannelRequest("#new-channel"), repo, new TestPublishEndpoint());

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await repo.GetInstallation(teamId);
        Assert.Null(updated.GetChannel("#old-channel"));
        Assert.Equal(123, (int)updated.GetChannel("#new-channel")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task MoveChannel_ChannelNotFound_ReturnsNotFound()
    {
        var teamId = await fixture.InstallSlackbot();
        var repo = fixture.Services.GetRequiredService<ISlackTeamRepository>();

        var result = await AdminSlackEndpoints.MoveChannel(teamId, "#missing", new MoveChannelRequest("#new-channel"), repo, new TestPublishEndpoint());

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task FollowLeague_SetsTheFollowedLeague()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);
        var leagueClient = A.Fake<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(999, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague { Properties = new ClassicLeagueProperties { Name = "New League" } });

        var result = await AdminSlackEndpoints.FollowLeague(
            teamId, "#fplbot", new FollowLeagueRequest(999), fixture.SlackRepo, leagueClient);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.Equal(999, (int)updated.GetChannel("#fplbot")!.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task FollowLeague_UnknownLeague_IsRejectedAndLeavesTheChannelUnchanged()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);
        var leagueClient = A.Fake<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(A<int>._, A<int>._, A<bool>._, A<int?>._)).Returns(Task.FromResult<ClassicLeague?>(null));

        var result = await AdminSlackEndpoints.FollowLeague(
            teamId, "#fplbot", new FollowLeagueRequest(404404), fixture.SlackRepo, leagueClient);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.Null(updated.GetChannel("#fplbot")!.FollowedLeagueId);
    }

    [Fact]
    public async Task UnfollowLeague_ClearsTheLeagueInRedis()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);
        var leagueClient = A.Fake<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(999, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague { Properties = new ClassicLeagueProperties { Name = "New League" } });
        await AdminSlackEndpoints.FollowLeague(teamId, "#fplbot", new FollowLeagueRequest(999), fixture.SlackRepo, leagueClient);

        var result = await AdminSlackEndpoints.UnfollowLeague(teamId, "#fplbot", fixture.SlackRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.Null(updated.GetChannel("#fplbot")!.FollowedLeagueId);
    }

    [Fact]
    public async Task AddChannel_SubscribesTheChannelToAllEvents()
    {
        var teamId = await fixture.InstallSlackbot();

        var result = await AdminSlackEndpoints.AddChannel(teamId, new AddChannelRequest("C0NEW00001"), fixture.SlackRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.True(updated.GetChannel("C0NEW00001")!.IsSubscribedTo(FplEvent.PriceChanges));
    }

    [Fact]
    public async Task AddChannel_AlreadySubscribed_ReturnsConflict()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);

        var result = await AdminSlackEndpoints.AddChannel(teamId, new AddChannelRequest("#fplbot"), fixture.SlackRepo);

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
    }

    [Fact]
    public async Task MoveChannel_PublishesSlackChannelMoved()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#old-channel", FplEvent.Standings);
        var publisher = new TestPublishEndpoint();

        await AdminSlackEndpoints.MoveChannel(teamId, "#old-channel", new MoveChannelRequest("C0NEW00001"), fixture.SlackRepo, publisher);

        var moved = Assert.Single(publisher.PublishedMessages.Containing<SlackChannelMoved>()).Message as SlackChannelMoved;
        Assert.Equal(teamId, moved!.TeamId);
        Assert.Equal("#old-channel", moved.OldChannelId);
        Assert.Equal("C0NEW00001", moved.NewChannelId);
    }

    [Fact]
    public async Task MoveChannel_TargetAlreadySubscribed_ReturnsConflictAndDoesNotPublish()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#old-channel", FplEvent.Standings);
        await fixture.Subscribe(teamId, "#taken", FplEvent.Standings);
        var publisher = new TestPublishEndpoint();

        var result = await AdminSlackEndpoints.MoveChannel(teamId, "#old-channel", new MoveChannelRequest("#taken"), fixture.SlackRepo, publisher);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, ((IStatusCodeHttpResult)result).StatusCode);
        Assert.Empty(publisher.PublishedMessages.Containing<SlackChannelMoved>());
        var updated = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.NotNull(updated.GetChannel("#old-channel"));
    }

    [Fact]
    public async Task GetAvailableChannels_ListsChannelsFromSlack()
    {
        var teamId = await fixture.InstallSlackbot();
        fixture.SetSlackChannels(
            new Conversation { Id = "C0ZULU00001", Name = "zulu", Is_Channel = true },
            new Conversation { Id = "C0ALPHA0001", Name = "alpha", Is_Channel = true });

        var slackClientBuilder = fixture.Services.GetRequiredService<Slackbot.Net.SlackClients.Http.ISlackClientBuilder>();
        var result = await AdminSlackEndpoints.GetAvailableChannels(teamId, fixture.SlackRepo, slackClientBuilder, NullLogger<Program>.Instance);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        var channels = ((IEnumerable<ChannelDto>)value).ToList();
        Assert.Equal(["alpha", "zulu"], channels.Select(c => c.Name));
        Assert.Equal("C0ALPHA0001", channels[0].Id);
    }

    [Fact]
    public async Task GetTeam_ResolvesChannelNameFromSlack()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0FPLBOT01", FplEvent.Standings);
        fixture.SetSlackChannels(new Conversation { Id = "C0FPLBOT01", Name = "fplbot", Is_Channel = true });

        var slackClientBuilder = fixture.Services.GetRequiredService<Slackbot.Net.SlackClients.Http.ISlackClientBuilder>();
        var result = await AdminSlackEndpoints.GetTeam(teamId, fixture.SlackRepo, A.Fake<ILeagueClient>(), slackClientBuilder, NullLogger<Program>.Instance);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        dynamic channel = Assert.Single((IEnumerable<object>)value.channels);
        Assert.Equal("fplbot", (string?)channel.channelName);
        Assert.True((bool?)channel.channelStatus);
    }

    [Fact]
    public async Task GetTeam_ChannelUnknownToSlack_HasNoChannelName()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "C0GONE0001", FplEvent.Standings);
        fixture.SetSlackChannels(new Conversation { Id = "C0FPLBOT01", Name = "fplbot", Is_Channel = true });

        var slackClientBuilder = fixture.Services.GetRequiredService<Slackbot.Net.SlackClients.Http.ISlackClientBuilder>();
        var result = await AdminSlackEndpoints.GetTeam(teamId, fixture.SlackRepo, A.Fake<ILeagueClient>(), slackClientBuilder, NullLogger<Program>.Instance);

        dynamic value = Assert.IsAssignableFrom<IValueHttpResult>(result).Value!;
        dynamic channel = Assert.Single((IEnumerable<object>)value.channels);
        Assert.Null((string?)channel.channelName);
        Assert.False((bool?)channel.channelStatus);
    }

    [Fact]
    public async Task GetTeam_IncludesFailureState()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        var failingSince = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        installation.GetChannel("#fplbot")!.RecordDeliveryFailure(failingSince, "not_in_channel");
        installation.GetChannel("#fplbot")!.RecordDeliveryFailure(failingSince.AddDays(1), "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var slackClientBuilder = fixture.Services.GetRequiredService<Slackbot.Net.SlackClients.Http.ISlackClientBuilder>();
        var result = await AdminSlackEndpoints.GetTeam(teamId, fixture.SlackRepo, A.Fake<ILeagueClient>(), slackClientBuilder, NullLogger<Program>.Instance);

        var ok = Assert.IsAssignableFrom<IValueHttpResult>(result);
        dynamic value = ok.Value!;
        dynamic channel = Assert.Single((IEnumerable<object>)value.channels);
        Assert.Equal(2, (int)channel.failureCount);
        Assert.Equal(failingSince, (DateTimeOffset?)channel.failingSince);
        Assert.Equal("not_in_channel", (string?)channel.lastFailureReason);
        Assert.Equal(failingSince + ChannelSubscription.MaxFailureAge, (DateTimeOffset?)channel.purgeEligibleAt);
        Assert.Equal(ChannelSubscription.MaxFailures - 2, (int)channel.failuresUntilPurge);
        Assert.Equal(ChannelSubscription.MaxFailures, (int)channel.purgeFailureLimit);
    }

    [Fact]
    public async Task GetTeams_FailingOnly_ExcludesHealthyTeams()
    {
        var failingTeamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(failingTeamId, "#fplbot", FplEvent.Standings);
        var healthyTeamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(healthyTeamId, "#fplbot", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(failingTeamId);
        installation.GetChannel("#fplbot")!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var result = await AdminSlackEndpoints.GetTeams(null, 1, 25, true, fixture.SlackRepo);

        var ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(result);
        Assert.Equal(failingTeamId, Assert.Single(ok.Value!.Items).TeamId);
        Assert.DoesNotContain(ok.Value.Items, t => t.TeamId == healthyTeamId);
    }

    [Fact]
    public async Task GetFailureStats_CountsFailingChannelsAndTeams()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        installation.GetChannel("#fplbot")!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var result = await AdminSlackEndpoints.GetFailureStats(fixture.SlackRepo);

        var ok = Assert.IsType<Ok<ChannelFailureStatsDto>>(result);
        Assert.Equal(1, ok.Value!.ChannelsWithFailures);
        Assert.Equal(1, ok.Value.InstallationsWithFailures);
        Assert.Equal(0, ok.Value.ChannelsEligibleForPurge);
    }

    [Fact]
    public async Task ResetFailures_ClearsCountersAcrossAllTeams()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);
        var installation = await fixture.SlackRepo.GetInstallation(teamId);
        installation.GetChannel("#fplbot")!.RecordDeliveryFailure(DateTimeOffset.UtcNow, "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        await AdminSlackEndpoints.ResetFailures(fixture.SlackRepo, NullLogger<Program>.Instance);

        var reloaded = await fixture.SlackRepo.GetInstallation(teamId);
        var channel = reloaded.GetChannel("#fplbot")!;
        Assert.Equal(0, channel.FailureCount);
        Assert.Null(channel.FailingSince);
        Assert.Null(channel.LastFailureReason);
    }

    [Fact]
    public async Task DeleteChannelSubscription_RemovesSubscriptionFromRepository()
    {
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, "#fplbot", FplEvent.Standings);

        var result = await AdminSlackEndpoints.DeleteChannelSubscription(teamId, "#fplbot", fixture.SlackRepo);

        Assert.IsAssignableFrom<IValueHttpResult>(result);
        var remaining = await fixture.SlackRepo.GetInstallation(teamId);
        Assert.DoesNotContain(remaining.ChannelSubscriptions, c => c.ChannelId == "#fplbot");
    }
}
