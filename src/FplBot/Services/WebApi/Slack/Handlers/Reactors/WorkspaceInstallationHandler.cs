using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Services.WebApi.Slack.Handlers.Reactors;

public class WorkspaceInstallationHandler(
    ISlackTeamRepository repository,
    IPublishEndpoint publisher,
    WorkspaceOwnerUninstallSlackWorkspace workspaceOwnerUninstallSlackWorkspace) : IWorkspaceInstallationHandler
{
    public async Task Install(Workspace workspace)
    {
        var installation = SlackInstallation.Install(workspace.TeamId, workspace.TeamName, workspace.Token);
        await repository.Save(installation);
        await publisher.Publish(new AppInstalled(workspace.TeamId, workspace.TeamName, ChatPlatform.Slack));
    }

    public Task Uninstall(string teamId) => workspaceOwnerUninstallSlackWorkspace.Execute(teamId);
}
