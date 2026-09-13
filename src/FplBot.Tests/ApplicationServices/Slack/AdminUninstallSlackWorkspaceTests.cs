using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.ApplicationServices.Slack;

[Collection("App")]
public class AdminUninstallSlackWorkspaceTests(AppFixture fixture) : IAsyncLifetime
{
    private ISlackTeamRepository Repo => fixture.Services.GetRequiredService<ISlackTeamRepository>();
    private readonly TestPublishEndpoint _publishEndpoint = new();

    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Execute_KnownTeam_MarksForRemovalSavesAndPublishes()
    {
        await Repo.Save(SlackInstallation.Install("T1", "Team One", "token1"));

        var sut = new AdminUninstallSlackWorkspace(Repo, _publishEndpoint);
        await sut.Execute("T1");

        var stored = await Repo.GetInstallation("T1");
        Assert.True(stored.PendingRemoval);
        Assert.False(stored.IsActive);

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<TeamMarkedForRemoval>());
        var evt = Assert.IsType<TeamMarkedForRemoval>(published.Message);
        Assert.Equal("T1", evt.TeamId);
    }
}
