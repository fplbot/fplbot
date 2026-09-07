using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers;

public class SlackWorkspaceUninstalledHandler(
    ISlackTeamRepository slackRepo,
    ISlackClientBuilder builder,
    IConfiguration config,
    ILogger<SlackWorkspaceUninstalledHandler> logger) : IConsumer<AppUninstalled>
{
    public async Task Consume(ConsumeContext<AppUninstalled> context)
    {
        var teamId = context.Message.TeamId;
        var teamName = context.Message.TeamName;

        logger.LogInformation("Deleting Slack team {TeamId} ({TeamName}) after uninstall", teamId, teamName);
        await slackRepo.DeleteByTeamId(teamId);

        var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
        var env = config.GetValue<string>("DOTNET_ENVIRONMENT");
        var prefix = env == "Production" ? "" : $"{env}: ";
        var client = builder.Build(token);
        await client.ChatPostMessage("#fplbot-notifications", $"{prefix}😔 '{teamName}' decided to uninstall @fplbot");
    }
}
