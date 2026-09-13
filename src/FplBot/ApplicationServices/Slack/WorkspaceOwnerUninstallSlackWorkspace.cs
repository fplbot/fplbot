using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.ApplicationServices.Slack;

public class WorkspaceOwnerUninstallSlackWorkspace(ISlackTeamRepository repository, IPublishEndpoint publisher)
{
    public async Task Execute(string teamId)
    {
        var team = await repository.FindByTeamId(teamId);
        if (team is null)
        {
            return;
        }

        var installation = SlackInstallationMapper.ToDomain(team);
        installation.Uninstall();
        await repository.DeleteByTeamId(team.TeamId!);
        await publisher.Publish(new AppUninstalled(team.TeamId!.ToUpper(), team.TeamName!));
    }
}
