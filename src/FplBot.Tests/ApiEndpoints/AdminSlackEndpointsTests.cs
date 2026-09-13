using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.WebApi.Endpoints.Api.Admin;
using SlackInstallation = FplBot.Domain.SlackInstallation;
using ClassicLeagueId = FplBot.Domain.ClassicLeagueId;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FplBot.Tests.ApiEndpoints;

public class AdminSlackEndpointsTests
{
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
        var installation = SlackInstallation.Install("T1", "Blank", "token1");
        installation.Subscribe("#fplbot", [FplBot.Domain.FplEvent.Standings]);
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.FindInstallationByTeamId("T1")).Returns(Task.FromResult<SlackInstallation?>(installation));

        var sendEndpointProvider = A.Fake<ISendEndpointProvider>();
        var gameweekClient = A.Fake<IGlobalSettingsClient>();

        var result = await AdminSlackEndpoints.PublishStandings(
            "T1", "#fplbot", repo, sendEndpointProvider, gameweekClient);

        dynamic value = Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result).Value!;
        Assert.False((bool)value.published);
        A.CallTo(() => sendEndpointProvider.GetSendEndpoint(A<Uri>._)).MustNotHaveHappened();
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
        var installation = SlackInstallation.Install("T1", "Blank", "token1");
        installation.Subscribe("#fplbot", [FplBot.Domain.FplEvent.Standings, FplBot.Domain.FplEvent.Captains]);
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.FindInstallationByTeamId("T1")).Returns(Task.FromResult<SlackInstallation?>(installation));

        var request = new UpdateChannelSubscriptionsRequest([EventSubscription.Captains, EventSubscription.Deadlines]);
        var result = await AdminSlackEndpoints.UpdateChannelSubscriptions("t1", "#fplbot", request, repo);

        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);
        var channel = installation.GetChannel("#fplbot")!;
        Assert.Equal(
            new[] { FplBot.Domain.FplEvent.Captains, FplBot.Domain.FplEvent.Deadlines }.OrderBy(e => e),
            channel.Events.Current.OrderBy(e => e));
        A.CallTo(() => repo.Save(installation)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateChannelSubscriptions_TeamNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.FindInstallationByTeamId("T1")).Returns(Task.FromResult<SlackInstallation?>(null));

        var result = await AdminSlackEndpoints.UpdateChannelSubscriptions("t1", "#fplbot", new UpdateChannelSubscriptionsRequest([]), repo);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task MoveChannel_MovesSubscriptionAndDeletesOldStorageKey()
    {
        var installation = SlackInstallation.Install("T1", "Blank", "token1");
        installation.Follow("#old-channel", new ClassicLeagueId(123));
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.FindInstallationByTeamId("T1")).Returns(Task.FromResult<SlackInstallation?>(installation));

        var result = await AdminSlackEndpoints.MoveChannel("t1", "#old-channel", new MoveChannelRequest("#new-channel"), repo);

        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);
        Assert.Null(installation.GetChannel("#old-channel"));
        Assert.Equal(123, (int)installation.GetChannel("#new-channel")!.FollowedLeagueId!.Value);
        A.CallTo(() => repo.DeleteChannelSubscription("T1", "#old-channel")).MustHaveHappenedOnceExactly();
        A.CallTo(() => repo.Save(installation)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task MoveChannel_ChannelNotFound_ReturnsNotFound()
    {
        var installation = SlackInstallation.Install("T1", "Blank", "token1");
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.FindInstallationByTeamId("T1")).Returns(Task.FromResult<SlackInstallation?>(installation));

        var result = await AdminSlackEndpoints.MoveChannel("t1", "#missing", new MoveChannelRequest("#new-channel"), repo);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task DeleteChannelSubscription_DelegatesToRepository()
    {
        var repo = A.Fake<ISlackTeamRepository>();

        var result = await AdminSlackEndpoints.DeleteChannelSubscription("t1", "#fplbot", repo);

        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);
        A.CallTo(() => repo.DeleteChannelSubscription("T1", "#fplbot")).MustHaveHappenedOnceExactly();
    }
}
