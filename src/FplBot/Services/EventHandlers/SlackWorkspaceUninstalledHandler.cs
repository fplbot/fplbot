using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers;

public class SlackWorkspaceUninstalledHandler(
    ISlackClientBuilder builder,
    IConfiguration config,
    ILogger<SlackWorkspaceUninstalledHandler> logger) : IConsumer<AppUninstalled>
{
    public async Task Consume(ConsumeContext<AppUninstalled> context)
    {
        var teamId = context.Message.TeamId;
        var teamName = context.Message.TeamName;

        logger.LogInformation("Notifying about Slack team {TeamId} ({TeamName}) uninstall", teamId, teamName);

        var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
        var env = config.GetValue<string>("DOTNET_ENVIRONMENT");
        var prefix = env == "Production" ? "" : $"{env}: ";
        var text = context.Message.Reason switch
        {
            UninstallReason.AutoPurged => $"{prefix}😔 fplbot gave up on '{teamName}' after repeated delivery failures",
            UninstallReason.AdminDeleted => $"{prefix}🗑️ '{teamName}' was removed by an admin",
            _ => $"{prefix}😔 '{teamName}' decided to uninstall @fplbot"
        };
        var client = builder.Build(token);
        await client.ChatPostMessage("#fplbot-notifications", text);
    }
}
