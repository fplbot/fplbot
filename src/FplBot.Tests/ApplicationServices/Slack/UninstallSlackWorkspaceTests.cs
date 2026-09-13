using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.ApplicationServices.Slack;

[Collection("App")]
public class WorkspaceOwnerUninstallSlackWorkspaceTests(AppFixture fixture) : IAsyncLifetime
{
    private ISlackTeamRepository Repo => fixture.Services.GetRequiredService<ISlackTeamRepository>();
    private readonly TestPublishEndpoint _publishEndpoint = new();

    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Execute_KnownTeam_DeletesAndPublishesAppUninstalled()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.Follow("#fpl", new ClassicLeagueId(42));
        installation.Subscribe("#fpl", [FplEvent.Standings]);
        await Repo.Save(installation);

        var sut = new WorkspaceOwnerUninstallSlackWorkspace(Repo, _publishEndpoint);
        await sut.Execute("T1");

        Assert.Null(await Repo.FindInstallationByTeamId("T1"));

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppUninstalled>());
        var appUninstalled = Assert.IsType<AppUninstalled>(published.Message);
        Assert.Equal("T1", appUninstalled.TeamId);
        Assert.Equal("Team One", appUninstalled.TeamName);
    }
}
