using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.WebApi.Endpoints.Api.Admin;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;

namespace FplBot.Tests.ApiEndpoints;

public class AdminSlackEndpointsTests
{
    private static SlackTeam Team(string id, string name) => new()
    {
        TeamId = id,
        TeamName = name,
        FplBotSlackChannel = "#fplbot",
        FplbotLeagueId = 123,
        Subscriptions = []
    };

    private static ISlackTeamRepository RepoWithTeams(params SlackTeam[] teams)
    {
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.GetAllTeams()).Returns(Task.FromResult<IEnumerable<SlackTeam>>(teams));
        return repo;
    }

    [Fact]
    public async Task GetTeams_FiltersByNameOrId_CaseInsensitive()
    {
        var repo = RepoWithTeams(
            Team("T1", "Blank"),
            Team("T2", "Other Workspace"),
            Team("T3", "Another blank one"));

        var result = await AdminSlackEndpoints.GetTeams("blank", 1, 25, repo, new MemoryCache(new MemoryCacheOptions()));

        var ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(result);
        Assert.Equal(2, ok.Value!.TotalCount);
        Assert.All(ok.Value.Items, t => Assert.Contains("blank", (t.TeamId + t.TeamName).ToLowerInvariant()));
    }

    [Fact]
    public async Task GetTeams_Paginates()
    {
        var teams = Enumerable.Range(1, 5).Select(i => Team($"T{i}", $"Team {i}")).ToArray();
        var repo = RepoWithTeams(teams);

        var page1 = await AdminSlackEndpoints.GetTeams(null, 1, 2, repo, new MemoryCache(new MemoryCacheOptions()));
        var page2 = await AdminSlackEndpoints.GetTeams(null, 2, 2, repo, new MemoryCache(new MemoryCacheOptions()));

        var page1Ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(page1);
        var page2Ok = Assert.IsType<Ok<PagedResult<TeamSummaryDto>>>(page2);
        Assert.Equal(5, page1Ok.Value!.TotalCount);
        Assert.Equal(2, page1Ok.Value.Items.Count);
        Assert.Equal(2, page2Ok.Value!.Items.Count);
        Assert.NotEqual(page1Ok.Value.Items[0].TeamId, page2Ok.Value.Items[0].TeamId);
    }

    [Fact]
    public async Task PublishTeamEvent_NoSubscriptionsSelected_DoesNotPublish()
    {
        var repo = A.Fake<ISlackTeamRepository>();
        var sendEndpointProvider = A.Fake<ISendEndpointProvider>();
        var gameweekClient = A.Fake<IGlobalSettingsClient>();

        var result = await AdminSlackEndpoints.PublishTeamEvent(
            "T1", new PublishEventRequest([]), repo, sendEndpointProvider, gameweekClient);

        dynamic value = Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result).Value!;
        Assert.False((bool)value.published);
        A.CallTo(() => sendEndpointProvider.GetSendEndpoint(A<Uri>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task PublishTeamEvent_StandingsSelected_PublishesToCorrectQueue()
    {
        var team = Team("T1", "Blank");
        var repo = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => repo.GetTeam("T1")).Returns(Task.FromResult(team));

        var sendEndpoint = A.Fake<ISendEndpoint>();
        var sendEndpointProvider = A.Fake<ISendEndpointProvider>();
        A.CallTo(() => sendEndpointProvider.GetSendEndpoint(A<Uri>._)).Returns(Task.FromResult(sendEndpoint));

        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(Task.FromResult<GlobalSettings?>(new GlobalSettings
        {
            Gameweeks = [new Gameweek { Id = 4, IsCurrent = true }]
        }));

        var result = await AdminSlackEndpoints.PublishTeamEvent(
            "t1", new PublishEventRequest([EventSubscription.Standings]), repo, sendEndpointProvider, gameweekClient);

        dynamic value = Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result).Value!;
        Assert.True((bool)value.published);
        A.CallTo(() => sendEndpointProvider.GetSendEndpoint(
            A<Uri>.That.Matches(u => u.ToString().Contains("SlackGameweekFinishedHandler")))).MustHaveHappenedOnceExactly();
    }
}
