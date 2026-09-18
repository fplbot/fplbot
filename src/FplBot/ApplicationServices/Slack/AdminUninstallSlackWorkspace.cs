using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.ApplicationServices.Slack;

// Marks the installation for removal and saves it - doesn't delete outright. Deleting here
// would mean publishing an event carrying the raw access token, since there'd be nothing left
// to look up afterward; AdminRequestedWorkspaceRemovalHandler re-fetches the (still-token-bearing)
// team, best-effort tells Slack, and only then does the actual delete.
public class AdminUninstallSlackWorkspace(ISlackTeamRepository repository, IPublishEndpoint publisher)
{
    public async Task Execute(string teamId)
    {
        var installation = await repository.GetInstallation(teamId);
        installation.MarkForRemoval();
        await repository.Save(installation);
        await publisher.Publish(new TeamMarkedForRemoval(installation.Id));
    }
}
