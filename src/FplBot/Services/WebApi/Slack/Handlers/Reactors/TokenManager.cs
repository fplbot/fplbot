using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Services.WebApi.Slack.Handlers.Reactors;

public class TokenManager(ISlackTeamRepository repository, IServiceScopeFactory scopeFactory) : ITokenStore
{
    public async Task Insert(Workspace workspace)
    {
        var installation = SlackInstallation.Install(workspace.TeamId, workspace.Token);
        await repository.Save(SlackInstallationMapper.ToStorage(installation, workspace.TeamName));

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
            .Publish(new AppInstalled(workspace.TeamId, workspace.TeamName, ChatPlatform.Slack));
    }

    public async Task Insert(SlackTeam slackTeam)
    {
        await repository.Save(slackTeam);
    }

    public async Task<Workspace?> Delete(string teamId)
    {
        var team = await repository.FindByTeamId(teamId);
        if (team is null)
        {
            return null;
        }

        var installation = SlackInstallationMapper.ToDomain(team);
        installation.Uninstall();

        await repository.DeleteByTeamId(team.TeamId!);
        return new Workspace(TeamId: team.TeamId!, TeamName: team.TeamName, Token: team.AccessToken ?? string.Empty);
    }
}
