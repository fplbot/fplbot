using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.ApplicationServices.Slack;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Tests.E2E;
using FplBot.WebApi.Endpoints.Api.Admin;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Response = Slackbot.Net.SlackClients.Http.Models.Responses.Response;

namespace FplBot.Tests.ApiEndpoints;

[Collection("App")]
public class AdminSlackEndpointsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Uninstall_SlackAcceptsUninstall_MarksForRemovalAndDeletesLocally()
    {
        var teamId = await fixture.InstallSlackbot();
        A.CallTo(() => fixture.SlackClient.AppsUninstall(A<string>._, A<string>._)).Returns(new Response { Ok = true });

        var result = await ExecuteUninstall(teamId);

        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);
        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    [Fact]
    public async Task Uninstall_SlackRejectsUninstall_StillDeletesLocally()
    {
        var teamId = await fixture.InstallSlackbot();
        A.CallTo(() => fixture.SlackClient.AppsUninstall(A<string>._, A<string>._)).Returns(new Response { Ok = false, Error = "something_broke" });

        await ExecuteUninstall(teamId);

        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    [Fact]
    public async Task Uninstall_SlackThrows_StillDeletesLocallyAndDoesNotCrash()
    {
        var teamId = await fixture.InstallSlackbot();
        A.CallTo(() => fixture.SlackClient.AppsUninstall(A<string>._, A<string>._))
            .Throws(new WellKnownSlackApiException(error: "account_inactive", responseContent: "{}"));

        await ExecuteUninstall(teamId);

        Assert.Null(await WaitForInstallationToBeGone(teamId));
    }

    private async Task<Microsoft.AspNetCore.Http.IResult> ExecuteUninstall(string teamId)
    {
        using var scope = fixture.Services.CreateScope();
        return await AdminSlackEndpoints.Uninstall(teamId, scope.ServiceProvider.GetRequiredService<AdminUninstallSlackWorkspace>(), NullLogger<Program>.Instance);
    }

    private async Task<SlackInstallation?> WaitForInstallationToBeGone(string teamId, TimeSpan? timeout = null)
    {
        var repository = fixture.Services.GetRequiredService<ISlackTeamRepository>();
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));

        SlackInstallation? installation;
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

    private static SlackInstallation Team(string id, string name) => SlackInstallation.Load(id, name, "token", []);

    private static ISlackTeamRepository RepoWithTeams(params SlackInstallation[] teams)
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
        var repo = fixture.Services.GetRequiredService<ISlackTeamRepository>();
        var installation = await repo.GetInstallation("T1");
        installation.Subscribe("#fplbot", [FplEvent.Standings]);
        await repo.Save(installation);

        var gameweekClient = A.Fake<IGlobalSettingsClient>();

        var result = await AdminSlackEndpoints.PublishStandings(
            "T1", "#fplbot", repo, fixture.Publisher, gameweekClient);

        dynamic value = Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result).Value!;
        Assert.False((bool)value.published);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(timeout: TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task PublishStandings_ChannelFollowingLeague_PublishesToCorrectQueue()
    {
        var installation = SlackInstallation.Install("T1", "Blank", "token1");
        installation.Follow("#fplbot", new ClassicLeagueId(123));
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.FindInstallationByTeamId("T1")).Returns(Task.FromResult<SlackInstallation?>(installation));

        var sendEndpoint = A.Fake<ISendEndpoint>();
        var sendEndpointProvider = A.Fake<ISendEndpointProvider>();
        A.CallTo(() => sendEndpointProvider.GetSendEndpoint(A<Uri>._)).Returns(Task.FromResult(sendEndpoint));

        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(Task.FromResult<GlobalSettings?>(new GlobalSettings
        {
            Gameweeks = [new Gameweek { Id = 4, IsCurrent = true }]
        }));

        var result = await AdminSlackEndpoints.PublishStandings(
            "t1", "#fplbot", repo, sendEndpointProvider, gameweekClient);

        dynamic value = Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result).Value!;
        Assert.True((bool)value.published);
        A.CallTo(() => sendEndpointProvider.GetSendEndpoint(
            A<Uri>.That.Matches(u => u.ToString().Contains("SlackGameweekFinishedHandler")))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_AddsAndRemovesToMatchRequestedSet()
    {
        var teamId = await fixture.InstallSlackbot();
        var repo = fixture.Services.GetRequiredService<ISlackTeamRepository>();
        await Subscribe(repo, teamId, "#fplbot", FplEvent.Standings, FplEvent.Captains);

        var request = new UpdateChannelSubscriptionsRequest([EventSubscription.Captains, EventSubscription.Deadlines]);
        var result = await AdminSlackEndpoints.UpdateChannelSubscriptions(teamId, "#fplbot", request, repo);

        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);
        var updated = await repo.GetInstallation(teamId);
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

        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);
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
    public async Task DeleteChannelSubscription_RemovesSubscriptionFromRepository()
    {
        var teamId = await fixture.InstallSlackbot();
        var repo = fixture.Services.GetRequiredService<ISlackTeamRepository>();
        await Subscribe(repo, teamId, "#fplbot", FplEvent.Standings);

        var result = await AdminSlackEndpoints.DeleteChannelSubscription(teamId, "#fplbot", repo);

        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);
        var remaining = await repo.GetChannelSubscriptions(teamId);
        Assert.DoesNotContain(remaining, c => c.ChannelId == "#fplbot");
    }

    private static async Task Subscribe(ISlackTeamRepository repo, string teamId, string channelId, params FplEvent[] events)
    {
        var installation = await repo.GetInstallation(teamId);
        installation.Subscribe(channelId, events);
        await repo.Save(installation);
    }
}
