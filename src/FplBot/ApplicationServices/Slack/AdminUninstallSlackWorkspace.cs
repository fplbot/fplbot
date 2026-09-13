using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.ApplicationServices.Slack;

// Marks the installation for removal and saves it - doesn't delete outright. Deleting here
// would mean publishing an event carrying the raw access token, since there'd be nothing left
// to look up afterward; AdminRequestedWorkspaceRemovalHandler re-fetches the (still-token-bearing)
// team, best-effort tells Slack, and only then does the actual delete.
public class AdminUninstallSlackWorkspace(ISlackTeamRepository repository, IServiceScopeFactory scopeFactory)
{
    public async Task<Workspace?> Execute(string teamId)
    {
        var team = await repository.GetTeam(teamId);
        if (team is null)
        {
            return null;
        }

        var installation = SlackInstallationMapper.ToDomain(team);
        installation.MarkForRemoval();
        await repository.Save(SlackInstallationMapper.ToStorage(installation, team.TeamName));

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
            .Publish(new TeamMarkedForRemoval(team.TeamId!, team.TeamName));

        return new Workspace(team.TeamId!, team.TeamName, team.AccessToken ?? string.Empty);
    }
}
