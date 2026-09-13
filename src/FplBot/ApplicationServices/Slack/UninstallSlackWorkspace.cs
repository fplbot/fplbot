using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.ApplicationServices.Slack;

public class UninstallSlackWorkspace(ISlackTeamRepository repository, IServiceScopeFactory scopeFactory)
{
    public async Task<Workspace?> Execute(string teamId)
    {
        var team = await repository.FindByTeamId(teamId);
        if (team is null)
        {
            return null;
        }

        var installation = SlackInstallationMapper.ToDomain(team);
        installation.Uninstall();

        await repository.DeleteByTeamId(team.TeamId!);

        var workspace = new Workspace(team.TeamId!, team.TeamName, team.AccessToken ?? string.Empty);

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
            .Publish(new AppUninstalled(workspace.TeamId.ToUpper(), workspace.TeamName));

        return workspace;
    }
}
