using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Services.WebApi.Slack.Handlers.Reactors;

public class TokenManager(ISlackTeamRepository repository, IServiceScopeFactory scopeFactory) : IWorkspaceInstallationHandler
{
    public async Task Install(Workspace workspace)
    {
        var installation = SlackInstallation.Install(workspace.TeamId, workspace.Token);
        await repository.Save(SlackInstallationMapper.ToStorage(installation, workspace.TeamName));

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
            .Publish(new AppInstalled(workspace.TeamId, workspace.TeamName, ChatPlatform.Slack));
    }

    public async Task<Workspace> Uninstall(string teamId)
    {
        var team = await repository.FindByTeamId(teamId);
        if (team is null)
        {
            return null;
        }

        var installation = SlackInstallationMapper.ToDomain(team);
        installation.Uninstall();

        await repository.DeleteByTeamId(team.TeamId!);

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
            .Publish(new AppUninstalled(team.TeamId!.ToUpper(), team.TeamName));

        return new Workspace(TeamId: team.TeamId!, TeamName: team.TeamName, Token: team.AccessToken ?? string.Empty);
    }
}
