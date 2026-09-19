using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Services.WebApi.Slack.Handlers.Reactors;

public class SlackbotNetInstallationBridge(
    ISlackTeamRepository repository,
    IPublishEndpoint publisher,
    ILogger<SlackbotNetInstallationBridge> logger
) : IWorkspaceInstallationHandler
{
    public async Task Install(Workspace workspace)
    {
        var existing = await repository.FindInstallationByTeamId(workspace.TeamId);
        var installation = existing is not null
            ? Installation.Reinstall(existing.Id, workspace.TeamId, workspace.TeamName, workspace.Token, existing.ChannelSubscriptions)
            : Installation.Install(workspace.TeamId, workspace.TeamName, workspace.Token);
        await repository.Save(installation);
        await publisher.Publish(new AppInstalled(workspace.TeamId, workspace.TeamName, ChatPlatform.Slack));
    }

    public async Task Uninstall(string teamId)
    {
        var installation = await repository.FindInstallationByTeamId(teamId);
        if (installation is null)
        {
            logger.LogWarning(
                "Uninstall called for teamId {teamId} but no installation found. Bot was already deleted by an admin",
                teamId);
            return;
        }

        installation.Uninstall();
        await repository.Delete(installation);
        await publisher.Publish(new AppUninstalled(installation.ExternalId, installation.Name));
    }
}
