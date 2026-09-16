using FakeItEasy;
using Fpl.Client.Abstractions;
using FplBot.ApplicationServices.Slack;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Slackbot.Net.SlackClients.Http.Exceptions;
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
        return await AdminSlackEndpoints.Uninstall(teamId, scope.ServiceProvider.GetRequiredService<AdminUninstallSlackWorkspace>(), NullLogger<Program>.Instance);
    }

    private async Task<Installation?> WaitForInstallationToBeGone(string teamId, TimeSpan? timeout = null)
    {
        var repository = fixture.Services.GetRequiredService<ISlackTeamRepository>();
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));

        Installation? installation;
        do
        {
            installation = await repository.FindInstallationByTeamId(teamId);
            if (installation is null)
            {
                return null;
            }
            await Task.Delay(50, cts.Token);
        } while (!cts.IsCancellationRequested);

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

        var result = await AdminSlackEndpoints.GetTeams("blank", 1, 25, repo);

        var ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(result);
        Assert.Equal(2, ok.Value!.TotalCount);
        Assert.All(ok.Value.Items, t => Assert.Contains("blank", (t.TeamId + t.TeamName).ToLowerInvariant()));
    }

    [Fact]
    public async Task GetTeams_Paginates()
    {
        var teams = Enumerable.Range(1, 5).Select(i => Team($"T{i}", $"Team {i}")).ToArray();
        var repo = RepoWithTeams(teams);

        var page1 = await AdminSlackEndpoints.GetTeams(null, 1, 2, repo);
        var page2 = await AdminSlackEndpoints.GetTeams(null, 2, 2, repo);

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

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
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

        var result = await AdminSlackEndpoints.UpdateChannelSubscriptions(Guid.NewGuid().ToString("N"), "#fplbot", new UpdateChannelSubscriptionsRequest([]), repo);

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

        var result = await AdminSlackEndpoints.MoveChannel(teamId, "#old-channel", new MoveChannelRequest("#new-channel"), repo);

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

        var result = await AdminSlackEndpoints.MoveChannel(teamId, "#missing", new MoveChannelRequest("#new-channel"), repo);

        Assert.IsType<NotFound>(result);
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
