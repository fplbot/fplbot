using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.ApplicationServices.Slack;

public class WorkspaceOwnerUninstallSlackWorkspace(ISlackTeamRepository repository, IPublishEndpoint publisher)
{
    public async Task Execute(string teamId)
    {
        var installation = await repository.GetInstallation(teamId);
        installation.Uninstall();
        await repository.DeleteByTeamId(installation.TeamId);
        await publisher.Publish(new AppUninstalled(installation.TeamId, installation.TeamName));
    }
}
